using PhotoshopFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PSDEditorWindow : EditorWindow
{
	private const string vers = "version 1.9";
	private Font font;
	private Texture2D image;
	private Vector2 scrollPos;
	private PsdFile psd;
	private int atlassize = 4096;
	private float pixelsToUnitSize = 100.0f;

	private string fileName;
	private List<string> LayerList = new List<string>();
	private bool ShowAtlas;
	private GameObject CanvasObj;
	private string PackingTag;

	private Vector2 defaultPivot;

	#region Const


	const string Button = "button";
	const string Highlight = "highlight";
	const string Disable = "disable";
	const string Touched = "pressed";

	#endregion

	#region MenuItems

	[MenuItem("Window/uGUI/PSD Converter")]
	public static void ShowWindow()
	{
		var wnd = GetWindow<PSDEditorWindow>();


		wnd.minSize = new Vector2(400, 300);
		wnd.Show();
	}




	[MenuItem("Assets/Convert to uGUI", true, 20000)]
	private static bool saveLayersEnabled()
	{
		for (var i = 0; i < Selection.objects.Length; i++)
		{


			var obj = Selection.objects[i];
			var filePath = AssetDatabase.GetAssetPath(obj);
			if (filePath.EndsWith(".psd", StringComparison.CurrentCultureIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	[MenuItem("Assets/Convert to uGUI", false, 20000)]
	private static void saveLayers()
	{


		var obj = Selection.objects[0];

		var window = GetWindow<PSDEditorWindow>(true, "PSD to uGUI " + vers);
		window.minSize = new Vector2(400, 300);
		window.image = (Texture2D)obj;
		window.LoadInformation(window.image);
		window.Show();





	}

	#endregion

	public void OnEnable()
	{

		var temppath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
		var temp = temppath.Split('/').ToList();

		temp.Remove(temp[temp.Count - 1]);
		temp.Remove(temp[temp.Count - 1]);
		temppath = "";
		foreach (var item in temp)
		{
			temppath += (item + "/");
		}
		titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(temppath + "Logo/logo.png");
		titleContent.text = "Parser";
		defaultPivot = new Vector2(0.5f, 0.5f);
	}

	public void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("PSD to UGUI " + vers);
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();


		GUILayout.Label("Select default font for UI");
		EditorGUILayout.Space();
		font = (Font)EditorGUILayout.ObjectField("Font", font, typeof(Font), true);
		EditorGUILayout.Space();

		GUILayout.Label("Packing Tag");
		PackingTag = EditorGUILayout.TextArea(PackingTag);

		EditorGUILayout.Space();

		defaultPivot = EditorGUILayout.Vector2Field("Set default pivot", defaultPivot);

		EditorGUILayout.Space();

		EditorGUI.BeginChangeCheck();
		image = (Texture2D)EditorGUILayout.ObjectField("PSD File", image, typeof(Texture2D), true);
		EditorGUILayout.Space();
		var changed = EditorGUI.EndChangeCheck();

		if (image != null)
		{
			if (changed)
			{
				var path = AssetDatabase.GetAssetPath(image);

				if (path.ToUpper().EndsWith(".PSD", StringComparison.CurrentCultureIgnoreCase))
				{
					LoadInformation(image);
				}
				else
				{
					psd = null;
				}

			}

			if (font == null)
			{
				EditorGUILayout.HelpBox("Choose the font of text layers.", MessageType.Error);
			}
			if (psd != null)
			{
				scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

				foreach (Layer layer in psd.Layers)
				{

					var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
												.SingleOrDefault(x => x is LayerSectionInfo);
					if (sectionInfo == null && layer.Rect.width > 0 && layer.Rect.height > 0)
					{

						layer.Visible = EditorGUILayout.ToggleLeft(layer.Name, layer.Visible);
					}

				}

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Select All", GUILayout.Width(200)))
				{
					foreach (Layer layer in psd.Layers)
					{
						var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
							.SingleOrDefault(x => x is LayerSectionInfo);
						if (sectionInfo == null)
						{

							layer.Visible = true;
						}

					}

				}
				if (GUILayout.Button("Select None", GUILayout.Width(200)))
				{
					foreach (Layer layer in psd.Layers)
					{
						var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
							.SingleOrDefault(x => x is LayerSectionInfo);
						if (sectionInfo == null)
						{

							layer.Visible = false;
						}

					}

				}
				EditorGUILayout.EndHorizontal();


				EditorGUILayout.EndScrollView();


				if (GUILayout.Button("Create atlas && GUI "))
				{

					ShowAtlas = !ShowAtlas;
				}
				if (ShowAtlas)
				{
					atlassize = EditorGUILayout.IntField("Max. atlas size", atlassize);

					if (!((atlassize != 0) && ((atlassize & (atlassize - 1)) == 0)))
					{
						EditorGUILayout.HelpBox("Atlas size should be a power of 2", MessageType.Warning);
					}

					pixelsToUnitSize = EditorGUILayout.FloatField("Pixels To Unit Size", pixelsToUnitSize);

					if (pixelsToUnitSize <= 0)
					{
						EditorGUILayout.HelpBox("Pixels To Unit Size should be greater than 0.", MessageType.Warning);
					}
					if (GUILayout.Button("Start"))
					{
						CreateAtlas();


					}
				}
				if (GUILayout.Button("Create Folder With Sprites && GUI"))
				{
					ExportLayers();
				}



			}
			else
			{
				EditorGUILayout.HelpBox("This texture is not a PSD file.", MessageType.Error);
			}
		}
	}

	private Texture2D CreateTexture(Layer layer)
	{
		if ((int)layer.Rect.width == 0 || (int)layer.Rect.height == 0)
		{
			return null;
		}

		var tex = new Texture2D((int)layer.Rect.width, (int)layer.Rect.height, TextureFormat.RGBA32, true);
		Color32[] pixels = new Color32[tex.width * tex.height];
		var red = (from l in layer.Channels
				   where l.ID == 0
				   select l).First();
		var green = (from l in layer.Channels
					 where l.ID == 1
					 select l).First();
		var blue = (from l in layer.Channels
					where l.ID == 2
					select l).First();
		var alpha = layer.AlphaChannel;
		for (int i = 0; i < pixels.Length; i++)
		{
			byte r = red.ImageData[i];
			byte g = green.ImageData[i];
			byte b = blue.ImageData[i];
			byte a = 255;
			if (alpha != null)
			{
				a = alpha.ImageData[i];
			}
			int mod = i % tex.width;
			int n = ((tex.width - mod - 1) + i) - mod;
			pixels[pixels.Length - n - 1] = new Color32(r, g, b, a);
		}

		tex.SetPixels32(pixels);
		tex.Apply();
		return tex;
	}

	private void CreateAtlas()
	{
		var textures = new List<Texture2D>();
		var spriteRenderers = new List<SpriteRenderer>();
		LayerList = new List<string>();
		int zOrder = 0;
		var root = new GameObject(fileName);
		foreach (var layer in psd.Layers)
		{
			if (layer.Visible && layer.Rect.width > 0 && layer.Rect.height > 0)
			{
				if (LayerList.IndexOf(layer.Name.Split('|').Last()) == -1)
				{
					LayerList.Add(layer.Name.Split('|').Last());
					var tex = CreateTexture(layer);
					textures.Add(tex);
					var go = new GameObject(layer.Name);
					var sr = go.AddComponent<SpriteRenderer>();
					go.transform.localPosition = new Vector3((layer.Rect.width / 2 + layer.Rect.x) / pixelsToUnitSize,
						(-layer.Rect.height / 2 - layer.Rect.y) / pixelsToUnitSize, 0);
					spriteRenderers.Add(sr);
					sr.sortingOrder = zOrder++;
					go.transform.parent = root.transform;
				}
			}
		}
		Rect[] rects;
		var atlas = new Texture2D(atlassize, atlassize);
		var textureArray = textures.ToArray();
		rects = atlas.PackTextures(textureArray, 2, atlassize);
		var Sprites = new List<SpriteMetaData>();
		for (int i = 0; i < rects.Length; i++)
		{
			var smd = new SpriteMetaData();
			smd.name = spriteRenderers[i].name.Split('|').Last();
			smd.rect = new Rect(rects[i].xMin * atlas.width,
				rects[i].yMin * atlas.height,
				rects[i].width * atlas.width,
				rects[i].height * atlas.height);
			smd.pivot = new Vector2(0.5f, 0.5f);
			smd.alignment = (int)SpriteAlignment.Center;
			Sprites.Add(smd);
		}

		// Need to load the image first
		var assetPath = AssetDatabase.GetAssetPath(image);
		var path = Path.Combine(Path.GetDirectoryName(assetPath),
					   Path.GetFileNameWithoutExtension(assetPath) + "_atlas" + ".png");

		var buf = atlas.EncodeToPNG();
		File.WriteAllBytes(path, buf);
		AssetDatabase.Refresh();
		// Get our texture that we loaded
		atlas = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
		var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
		// Make sure the size is the same as our atlas then create the spritesheet
		textureImporter.maxTextureSize = atlassize;
		textureImporter.spritesheet = Sprites.ToArray();
		textureImporter.textureType = TextureImporterType.Sprite;
		textureImporter.spriteImportMode = SpriteImportMode.Multiple;
		textureImporter.spritePivot = new Vector2(0.5f, 0.5f);
		textureImporter.spritePixelsPerUnit = pixelsToUnitSize;
		textureImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
		AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
		foreach (Texture2D tex in textureArray)
		{
			DestroyImmediate(tex);
		}
		AssetDatabase.Refresh();
		DestroyImmediate(root);
		var atlases = AssetDatabase.LoadAllAssetsAtPath(path).Select(x => x as Sprite).Where(x => x != null).ToArray();
		CreateGUI(atlases);

	}

	private void CreateGUI(Sprite[] atlas)
	{
		int j = 0;
		int currentid = 0;
		for (int i1 = 0; i1 < psd.Layers.Count; i1++)
		{
			if (((LayerSectionInfo)psd.Layers[i1].AdditionalInfo
				.SingleOrDefault(x => x is LayerSectionInfo) == null) && (psd.Layers[i1].Visible) && psd.Layers[i1].Rect.width > 0 && psd.Layers[i1].Rect.height > 0)
			{
				j++;
			}
		}
		try
		{

			var objects = new List<GameObject>();

			for (int i = 0; i < psd.Layers.Count; i++)
			{
				if (((LayerSectionInfo)psd.Layers[i].AdditionalInfo
					.SingleOrDefault(x => x is LayerSectionInfo) == null) && (psd.Layers[i].Visible) && psd.Layers[i].Rect.width > 0 && psd.Layers[i].Rect.height > 0)
				{
					currentid++;
					var RealLayerName = psd.Layers[i].Name.Split('|').Last();

					string infoString = string.Format("Exporting {0} / {1} Layers", currentid, j);
					string fileString = string.Format("Exporting PSD Layer: {0}", RealLayerName);
					EditorUtility.DisplayProgressBar(fileString, infoString, currentid / j);
					var textlayer = (LayerTextInfo)psd.Layers[i].AdditionalInfo.SingleOrDefault(x => x is LayerTextInfo);

					if (RealLayerName.ToLower().StartsWith(Button) && RealLayerName.ToLower().EndsWith(Touched))
					{
						var temp = GameObject.Find(RealLayerName.Split('_')[0] + "_" + RealLayerName.Split('_')[1]);
						var State = temp.GetComponent<Selectable>().spriteState;
						State.pressedSprite = atlas[Array.FindIndex(atlas, x => x.name == RealLayerName)];
						temp.GetComponent<Selectable>().spriteState = State;
					}
					else if (RealLayerName.ToLower().StartsWith(Button) && RealLayerName.ToLower().EndsWith(Highlight))
					{
						var temp = GameObject.Find(RealLayerName.Split('_')[0] + "_" + RealLayerName.Split('_')[1]);
						var State = temp.GetComponent<Selectable>().spriteState;
						State.highlightedSprite = atlas[Array.FindIndex(atlas, x => x.name == RealLayerName)];
						temp.GetComponent<Selectable>().spriteState = State;
					}
					else if (RealLayerName.ToLower().StartsWith(Button) && RealLayerName.ToLower().EndsWith(Disable))
					{
						var temp = GameObject.Find(RealLayerName.Split('_')[0] + "_" + RealLayerName.Split('_')[1]);
						var State = temp.GetComponent<Selectable>().spriteState;
						State.disabledSprite = atlas[Array.FindIndex(atlas, x => x.name == RealLayerName)];
						temp.GetComponent<Selectable>().spriteState = State;
					}
					else
					{

						var instant = CreatePanel(psd.Layers[i].Name.Split('|'));
						instant.name = psd.Layers[i].Name.Split('|').Last();
						instant.SetActive(false);


						if (textlayer != null)
						{

							instant.AddComponent<Text>();
							instant.GetComponent<Text>().text = textlayer.text;
							instant.GetComponent<Text>().color = ColorPicker(psd.Layers[i]);
							instant.GetComponent<Text>().font = font;
							instant.GetComponent<RectTransform>().sizeDelta = new Vector2(atlas[Array.FindIndex(atlas, x => x.name == RealLayerName)].rect.width, atlas[Array.FindIndex(atlas, x => x.name == RealLayerName)].rect.height);
							instant.GetComponent<Text>().resizeTextForBestFit = true;

						}
						else
						{

							instant.AddComponent<Image>();
							instant.GetComponent<Image>().sprite = atlas[Array.FindIndex(atlas, x => x.name == RealLayerName)];
							instant.GetComponent<Image>().SetNativeSize();
						}

						instant.SetActive(true);

						instant.GetComponent<RectTransform>().anchorMin = Vector2.zero;
						instant.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
						instant.GetComponent<RectTransform>().pivot = Vector2.zero;
						instant.GetComponent<RectTransform>().anchorMax = Vector2.zero;
						instant.transform.localPosition = new Vector3(psd.Layers[i].Rect.xMin - image.width / 2, image.height / 2 - psd.Layers[i].Rect.yMax, 0);
						var tempPos = instant.transform.position;


						instant.transform.position = tempPos;
						if (RealLayerName.ToLower().StartsWith(Button))
						{
							instant.name = RealLayerName.Split('_')[0] + "_" + RealLayerName.Split('_')[1];
							instant.AddComponent<Button>().transition = Selectable.Transition.SpriteSwap;
						}

						objects.Add(instant);
					}

				}

			}
			foreach (var item in objects)
			{
				SetPivot(item.GetComponent<RectTransform>(), defaultPivot);
				SetAnchor(item.GetComponent<RectTransform>());
				if (item.GetComponent<Text>() != null)
				{
					DeleteAsset(item.name);
				}
			}
		}
		catch (Exception e)
		{
			Debug.Log(e);
			EditorUtility.ClearProgressBar();
		}

		EditorUtility.ClearProgressBar();


	}

	GameObject CreatePanel(string[] path)
	{
		var pathtemp = new List<string>();
		pathtemp.Add("Canvas");
		pathtemp.AddRange(path);
		CanvasObj = GameObject.Find("Canvas");
		var PathObj = new List<GameObject>();
		if (CanvasObj == null)
		{
			CanvasObj = new GameObject();
			CanvasObj.name = "Canvas";
			CanvasObj.AddComponent<Canvas>();
			Debug.Log("Created new Canvas");

		}
		PathObj.Add(CanvasObj);

		for (int i = 1; i < pathtemp.Count - 1; i++)
		{

			if (PathObj[i - 1].transform.Find(pathtemp[i]) == null)
			{
				var temp = new GameObject();
				temp.SetActive(false);
				temp.name = pathtemp[i];
				temp.transform.SetParent(PathObj[i - 1].transform);
				temp.AddComponent<RectTransform>();
				temp.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
				temp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.5f, 0.5f);
				temp.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
				temp.GetComponent<RectTransform>().sizeDelta = new Vector2(image.width, image.height);
				PathObj.Add(temp);
				temp.SetActive(true);

			}
			else
			{
				PathObj.Add(PathObj[i - 1].transform.Find(pathtemp[i]).gameObject);
			}
		}
		var temp1 = new GameObject();
		temp1.SetActive(false);
		temp1.AddComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
		temp1.name = pathtemp[pathtemp.Count - 1];
		temp1.transform.GetComponent<RectTransform>().SetParent(PathObj[pathtemp.Count - 2].transform);
		PathObj.Add(temp1);
		temp1.SetActive(true);



		return PathObj.Last();

	}

	public static void ApplyLayerSections(List<Layer> layers)
	{
		var stack = new Stack<string>();


		foreach (var layer in Enumerable.Reverse(layers))
		{

			var sectionInfo = (LayerSectionInfo)layer.AdditionalInfo
								.SingleOrDefault(x => x is LayerSectionInfo);
			if (sectionInfo == null)
			{
				var Reverstack = stack.ToArray();
				Array.Reverse(Reverstack);
				layer.Name = string.Join("|", Reverstack) + "|" + layer.Name;
			}
			else
			{
				switch (sectionInfo.SectionType)
				{
					case LayerSectionType.OpenFolder:


						stack.Push(layer.Name);
						break;
					case LayerSectionType.Layer:
						stack.Push(layer.Name);
						break;
					case LayerSectionType.ClosedFolder:

						stack.Push(layer.Name);

						break;
					case LayerSectionType.SectionDivider:


						stack.Pop();
						break;
				}


			}
		}

	}


	private void ExportLayers()
	{
		LayerList = new List<string>();
		var atlas = new List<Sprite>();

		string path = AssetDatabase.GetAssetPath(image).Split('.')[0];

		Directory.CreateDirectory(path);
		foreach (Layer layer in psd.Layers)
		{
			if (layer.Visible && layer.Rect.width > 0 && layer.Rect.height > 0)
			{
				if (LayerList.IndexOf(layer.Name.Split('|').Last()) == -1)
				{
					LayerList.Add(layer.Name.Split('|').Last());
					var tex = CreateTexture(layer);
					if (tex == null)
					{
						continue;
					}
					atlas.Add(SaveAsset(tex, layer.Name.Split('|').Last()));
					DestroyImmediate(tex);
				}
			}
		}
		CreateGUI(atlas.ToArray());
	}

	private Sprite SaveAsset(Texture2D tex, string syffux)
	{
		string path = AssetDatabase.GetAssetPath(image).Split('.')[0] + "/" + syffux + ".png";

		byte[] buf = tex.EncodeToPNG();
		File.WriteAllBytes(path, buf);
		AssetDatabase.Refresh();
		// Load the texture so we can change the type
		AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
		TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

		textureImporter.textureType = TextureImporterType.Sprite;
		textureImporter.spriteImportMode = SpriteImportMode.Single;
		textureImporter.maxTextureSize = atlassize;
		if (!System.String.IsNullOrEmpty(PackingTag))
		{
			textureImporter.spritePackingTag = PackingTag;
		}

		textureImporter.textureFormat = TextureImporterFormat.AutomaticTruecolor;
		AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		return (Sprite)AssetDatabase.LoadAssetAtPath(path, typeof(Sprite));
	}

	private void DeleteAsset(string syffux)
	{
		string path = AssetDatabase.GetAssetPath(image).Split('.')[0] + "/" + syffux + ".png";
		AssetDatabase.DeleteAsset(path);
	}

	public void LoadInformation(Texture2D Img)
	{
		string path = AssetDatabase.GetAssetPath(Img);

		psd = new PsdFile(path, Encoding.Default);
		fileName = Path.GetFileNameWithoutExtension(path);
		ApplyLayerSections(psd.Layers);

	}

	public Color32 ColorPicker(Layer layer)
	{
		Color32[] pixels = new Color32[(int)layer.Rect.width * (int)layer.Rect.height];
		var red = (from l in layer.Channels
				   where l.ID == 0
				   select l).First();
		var green = (from l in layer.Channels
					 where l.ID == 1
					 select l).First();
		var blue = (from l in layer.Channels
					where l.ID == 2
					select l).First();
		Channel alpha = layer.AlphaChannel;
		for (int i = 0; i < pixels.Length; i++)
		{
			byte r = red.ImageData[i];
			byte g = green.ImageData[i];
			byte b = blue.ImageData[i];
			byte a = 255;
			if (alpha != null)
			{
				a = alpha.ImageData[i];
			}
			int mod = i % (int)layer.Rect.width;
			int n = (((int)layer.Rect.width - mod - 1) + i) - mod;
			pixels[pixels.Length - n - 1] = new Color32(r, g, b, a);
		}
		int r1 = 0;
		int g1 = 0;
		int b1 = 0;
		byte a1 = 255;
		pixels.ToList().ForEach(delegate (Color32 name)
		{
			r1 += name.r;
			g1 += name.g;
			b1 += name.b;
		}
		);
		return new Color32((byte)(r1 / pixels.Count()), (byte)(g1 / pixels.Count()), (byte)(b1 / pixels.Count()), a1);

	}

	public void SetPivot(RectTransform rectTransform, Vector2 pivot)
	{
		if (rectTransform == null) return;

		Vector2 size = rectTransform.rect.size;
		Vector2 deltaPivot = rectTransform.pivot - pivot;
		Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
		rectTransform.pivot = pivot;
		rectTransform.localPosition -= deltaPosition;
	}

	public void SetAnchor(RectTransform rectTransform)
	{
		SetAnchor(rectTransform, new Vector2(0.5f, 0.5f));
	}

	public void SetAnchor(RectTransform rectTransform, Vector2 point)
	{
		if (rectTransform == null) return;


		var pos = rectTransform.localPosition;
		rectTransform.anchorMin = point;
		rectTransform.anchorMax = point;
		rectTransform.localPosition = pos;

	}

}




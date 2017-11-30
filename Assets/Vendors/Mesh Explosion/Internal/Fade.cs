#if !(UNITY_2_6 || UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)
#define UNITY_4_OR_ABOVE
#endif

#if UNITY_4_OR_ABOVE && !(UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6)
#define UNITY_5_OR_ABOVE
#endif

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Fade : MonoBehaviour {

	public Material[] materials;
	public float waitTime = 0;
	public float fadeTime = 4;
	public bool replaceShaders = true;
	
	static Dictionary<Shader, Shader> replacementShaders = new Dictionary<Shader, Shader>();

#if UNITY_5_OR_ABOVE
	static string[] standardShaderNames = new string[] { "Standard", "Standard (Specular setup)" };

	static bool IsStandardShader(Shader shader) {
		return System.Array.IndexOf(standardShaderNames, shader.name) != -1;
	}
#endif

	public static Shader GetReplacementFor(Shader original) {
		Shader replacement;
		if (replacementShaders.TryGetValue(original, out replacement)) return replacement;

		const string legacyShadersPrefix = "Legacy Shaders/";
		const string transparentPrefix = "Transparent/";
		const string mobilePrefix = "Mobile/";

#if UNITY_5_OR_ABOVE
		if (IsStandardShader(original)) {
			replacement = original;
		} else
#endif
		{
			var name = original.name;

			var originalIsLegacy = name.StartsWith(legacyShadersPrefix);
			if (originalIsLegacy) {
				name = name.Substring(legacyShadersPrefix.Length);
			}

			if (name.StartsWith(mobilePrefix)) {
				name = name.Substring(mobilePrefix.Length);
			}
			
			if (name.StartsWith(transparentPrefix)) {
				replacement = original;
			} else {
				name = transparentPrefix + name;
				if (originalIsLegacy) name = legacyShadersPrefix + name;
				replacement = Shader.Find(name);
			}
		}

		replacementShaders[original] = replacement;
		return replacement;
	}
	
	static string[] colorPropertyNameCandidates = new string[] { "_Color", "_TintColor" };
	
	IEnumerator Start() {
		var mat = materials;
		if (mat == null || mat.Length == 0) materials = mat = GetComponent<Renderer>().materials;
		
		if (waitTime > 0) yield return new WaitForSeconds(waitTime);
		
		if (replaceShaders) {
			foreach (var i in mat) {
				var replacement = GetReplacementFor(i.shader);
				if (replacement != null) i.shader = replacement;
			}
		}
		
		var materialCount = mat.Length;
		List<string> colorPropertyNames = new List<string>(materialCount);
		
		foreach (var m in mat) {
#if UNITY_5_OR_ABOVE
			if (IsStandardShader(m.shader)) {
				// Set it to "Fade" mode:
				m.SetFloat("_Mode", 2);

				var material = m;

				// From StandardShaderGUI.SetupMaterialWithBlendMode() in Unity's 'Built-in Shaders' package:
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
			}
#endif

			{
				var found = false;
				foreach (var candidate in colorPropertyNameCandidates) {
					found = m.HasProperty(candidate);
					if (found) {
						colorPropertyNames.Add(candidate);
						break;
					}
				}
				
				if (!found) {
					colorPropertyNames.Add(null);
				}
			}
		}
		
		for (float t = 0; t < fadeTime; t += Time.deltaTime) {
			for (var i = 0; i < materialCount; ++i) {
				var m = mat[i];
				var colorPropertyName = colorPropertyNames[i];
				if (colorPropertyName == null) continue;
				
				var c = m.GetColor(colorPropertyName);
				c.a = 1 - (t / fadeTime);
				m.SetColor(colorPropertyName, c);
			}
			yield return null;
		}
		
		SendMessage("FadeCompleted", SendMessageOptions.DontRequireReceiver);
	}

}

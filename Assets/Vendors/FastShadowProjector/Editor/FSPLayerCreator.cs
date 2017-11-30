using UnityEngine;
using UnityEditor;
using System.Collections;

[InitializeOnLoad]
public class FSPLayerCreator
{
	static FSPLayerCreator()
	{
		if (!ShadowProjectorEditor.GlobalProjectorLayerExists()) 
		{
			ShadowProjectorEditor.CheckGlobalProjectorLayer();
		}
	}
}

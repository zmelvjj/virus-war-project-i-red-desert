using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace SpaceShockwaveURP {
public static class SpaceShockwaveSetup {
 const string Folder="Assets/SpaceShockwaveURP/";
 [MenuItem("Tools/Space Shockwave URP/Create Effect")]
 public static void Create() {
  var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
  if(pipeline==null) { Debug.LogError("Select an active URP pipeline first."); return; }
  Undo.RecordObject(pipeline,"Enable Opaque Texture");
  pipeline.supportsCameraOpaqueTexture=true; EditorUtility.SetDirty(pipeline);
  if(Camera.main!=null) {
   var data=Camera.main.GetUniversalAdditionalCameraData();
   Undo.RecordObject(data,"Enable camera Opaque Texture");
   data.requiresColorOption=CameraOverrideOption.On; EditorUtility.SetDirty(data);
  }
  var shader=Shader.Find("SpaceShockwaveURP/Refraction");
  if(shader==null) { Debug.LogError("Shockwave shader missing."); return; }
  var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"SpaceShockwave.mat");
  if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,Folder+"SpaceShockwave.mat"); }
  material.shader=shader; EditorUtility.SetDirty(material);
  var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name="Space Shockwave URP";
  Object.DestroyImmediate(go.GetComponent<Collider>());
  go.transform.rotation=Quaternion.Euler(90,0,0); go.transform.position=new Vector3(0,.08f,0);
  var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=material;
  renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
  go.AddComponent<SpaceShockwave>();
  PrefabUtility.SaveAsPrefabAsset(go,Folder+"SpaceShockwave.prefab");
  Undo.RegisterCreatedObjectUndo(go,"Create space shockwave"); Selection.activeGameObject=go;
  AssetDatabase.SaveAssets();
 }
}
}

using UnityEngine;
namespace SpaceShockwaveURP {
[RequireComponent(typeof(MeshRenderer))]
public sealed class SpaceShockwave : MonoBehaviour {
 [Min(.05f)] public float duration = 1.1f;
 [Min(.1f)] public float diameter = 7f;
 [Range(0,80)] public float distortionPixels = 22f;
 [Range(.02f,.3f)] public float waveWidth = .12f;
 [Range(0,1)] public float edgeLight = .025f;
 public bool loop;
 public bool destroyOnFinish;
 MeshRenderer target;
 MaterialPropertyBlock block;
 float time;
 void OnEnable() { target=GetComponent<MeshRenderer>(); if(block==null) block=new MaterialPropertyBlock(); time=0; target.enabled=true; Apply(0); }
 public void Play() { if(target==null) target=GetComponent<MeshRenderer>(); if(block==null) block=new MaterialPropertyBlock(); time=0; enabled=true; target.enabled=true; Apply(0); }
 void Update() {
  time+=Time.deltaTime;
  float life=Mathf.Max(.05f,duration);
  if(time>=life) {
   if(loop) time%=life;
   else { target.enabled=false; enabled=false; if(destroyOnFinish) Destroy(gameObject); return; }
  }
  Apply(time/life);
 }
 void Apply(float progress) {
  transform.localScale=Vector3.one*Mathf.Max(.1f,diameter);
  target.GetPropertyBlock(block);
  block.SetFloat("_Progress",progress); block.SetFloat("_Strength",distortionPixels);
  block.SetFloat("_Width",waveWidth); block.SetFloat("_Glow",edgeLight);
  target.SetPropertyBlock(block);
 }
}
}

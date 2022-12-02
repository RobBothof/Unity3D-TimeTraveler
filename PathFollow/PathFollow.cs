//using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
//using UnityEditor;

[System.Serializable]
public class controlPoint {
	public Vector3 pos;
	public bool selected;

	public controlPoint(Vector3 cpos, bool cselected) {
		pos=cpos;
		selected=cselected;
	}
}

[System.Serializable]
public class splinePoint {
    public Vector3 pos;
    public float len;
    public float sumlen;
}

[System.Serializable]
[CreateAssetMenu(fileName = "PathFollowClip", menuName = "Clips/PathFollow", order = 1)]
public class PathFollow : PlayableAsset, ITimelineClipAsset
{
	public ExposedReference<Transform> TransformBinding;
	public PlayableGraph TL;
	public Color pathColor;
	public PathFollowBehaviour data = new PathFollowBehaviour();
	public string clipName;
	public ClipCaps clipCaps {
        get { return ClipCaps.None;}
    }
	public bool EditorShowControlPoints = false;
	public bool loop=false;
	public bool normalized=true;
	public float tension=0.5f;
	public float resolution=0.25f;
	public float normalization=1.0f;
	public List<controlPoint> controlPoints = new List<controlPoint>();
	public List<splinePoint> splinePoints = new List<splinePoint>();
	//public List<Vector3> normalizedPoints = new List<Vector3>();
    public float splineLength;
	public bool created=false;

	public Vector3 beforePoint;
	public Vector3 afterPoint;
	

	public List<controlPoint> subDivide() {
	    List<controlPoint> tmp = new List<controlPoint>(controlPoints.ToArray());
		if (controlPoints.Count > 1) {
			for (int c=controlPoints.Count-2;c >=0 ; c--) {
				int last = controlPoints.Count - 1;
				int previous = (c == 0) ? 0 : c - 1;
				int start = c;
				int end = (c == last) ? last : c + 1;
				int next = (end == last) ? last : end + 1;

				if (controlPoints[start].selected && controlPoints[end].selected) {
					Vector3 p0 = controlPoints[previous].pos;
					Vector3 p1 = controlPoints[start].pos;
					Vector3 p2 = controlPoints[end].pos;
					Vector3 p3 = controlPoints[next].pos;
					Vector3 newPos = CatmullRom (0.5f, p0, p1, p2, p3, tension);
					tmp.Insert(end, new controlPoint(newPos,true));
				}
			}
			//controlPoints = new List<controlPoint>(tmp.ToArray());
		}
		if (loop) {
			if (controlPoints[0].selected && controlPoints[controlPoints.Count-1].selected) {
				int last = controlPoints.Count-1;
				Vector3 p0 = controlPoints[last-1].pos;
				Vector3 p1 = controlPoints[last].pos;
				Vector3 p2 = controlPoints[0].pos;
				Vector3 p3 = controlPoints[1].pos;
				Vector3 newPos = CatmullRom (0.5f, p0, p1, p2, p3, tension);
				tmp.Add(new controlPoint(newPos,true));
			}
		}
		return tmp;
	}
	public void buildPath() {
		if (data.pathPoints == null) {
			data.pathPoints = new List<Vector3>();
		}
		data.pathPoints.Clear();
		
		if (splinePoints.Count > 0) {
			float segmentLength = splineLength / (float) (splinePoints.Count-1);
			float nextLength=0;
			int currentpoint=0;
			//TODO no pathpoints when there is no distance
			if (splinePoints.Count > 1) {
				for (int x = 1; x < splinePoints.Count; x++) {
					while  (nextLength < splinePoints[x].sumlen) {
						//normalizedPoints.Add(Vector3.Lerp(splinePoints[x-1].pos,splinePoints[x].pos, (nextLength-splinePoints[x-1].sumlen) /  splinePoints[x].len));
						data.pathPoints.Add(Vector3.Lerp(splinePoints[currentpoint].pos, Vector3.Lerp(splinePoints[x-1].pos,splinePoints[x].pos, (nextLength-splinePoints[x-1].sumlen) /  splinePoints[x].len) , normalization));
						currentpoint++;
						nextLength = segmentLength*currentpoint;
					}
				}
				if (data.pathPoints.Count < splinePoints.Count) {
					data.pathPoints.Add(splinePoints[splinePoints.Count-1].pos);
				}
			} else {
				data.pathPoints.Add(splinePoints[0].pos);
			}
		}
	}
    public void buildSpline() {
		if (controlPoints.Count > 0) {
			if (splinePoints == null) {
				splinePoints = new List<splinePoint>();
			}
			splinePoints.Clear();
			splineLength=0;

			splinePoint sp1 = new splinePoint();
			sp1.pos = controlPoints[0].pos;
			sp1.len = 0;
			sp1.sumlen = 0;
			splinePoints.Add (sp1);
			Vector3 lastPos = sp1.pos;

			if (loop) {
				for (int c=0;c < controlPoints.Count; c++) {
					int last = controlPoints.Count - 1;
					int previous = (c == 0) ? last : c - 1;
					int start = c;
					int end = (c == last) ? 0 : c + 1;
					int next = (end == last) ? 0 : end + 1;

					Vector3 p0 = controlPoints[previous].pos;
					Vector3 p1 = controlPoints[start].pos;
					Vector3 p2 = controlPoints[end].pos;
					Vector3 p3 = controlPoints[next].pos;

					float dist = Vector3.Distance(p1,p2);
					float steps = Mathf.Round ((40*resolution) + 1 + (dist*resolution)*0.25f);

					//float steps = (int) (Vector3.Distance (p1, p2) * resolution) + 1;
					for (int t = 0; t <= steps; t += 1) {
						Vector3 newPos = CatmullRom (t / steps, p0, p1, p2, p3, tension);
		
						if (t == 0) {
							lastPos = newPos;
						}
				
						float len = Vector3.Distance (newPos, lastPos);
						if ((len !=0)) {
							splinePoint sp = new splinePoint();
							sp.pos = newPos;
							sp.len = len;
							splineLength += sp.len;
							sp.sumlen = splineLength;
							splinePoints.Add (sp);
							lastPos = newPos;
						} 
					}
				}
			} else {
				int last = controlPoints.Count - 1;
				beforePoint = CatmullRom (0.85f, controlPoints[0].pos, controlPoints[0].pos, controlPoints[1].pos, controlPoints[1].pos, 0.0f);
				afterPoint = CatmullRom (0.85f, controlPoints[last].pos, controlPoints[last].pos, controlPoints[last-1].pos, controlPoints[last-1].pos, 0.0f);

					
				for (int c=0;c < controlPoints.Count-1; c++) {
					int previous = (c == 0) ? 0 : c - 1;
					int start = c;
					int end = (c == last) ? last : c + 1;
					int next = (end == last) ? last : end + 1;

						
					Vector3 p0 = controlPoints[previous].pos;
					Vector3 p1 = controlPoints[start].pos;
					Vector3 p2 = controlPoints[end].pos;
					Vector3 p3 = controlPoints[next].pos;

					if (c==0) {
						p0 = beforePoint;
					}

					if (c == controlPoints.Count-2) {
						p3 = afterPoint;
					}

					float dist = Vector3.Distance(p1,p2);
					float steps = Mathf.Round ((40*resolution) + 1 + (dist*resolution)*0.25f);
					for (int t = 0; t <= steps; t += 1) {
						Vector3 newPos = CatmullRom (t / steps, p0, p1, p2, p3, tension);
						if (t == 0) {
							lastPos = newPos;
						}
						float len = Vector3.Distance (newPos, lastPos);
						if ((len !=0)) {
							splinePoint sp = new splinePoint();
							sp.pos = newPos;
							sp.len = len;
							splineLength += sp.len;
							sp.sumlen = splineLength;
							splinePoints.Add (sp);
							lastPos = newPos;
						} 
					}
				}
				
			}
		}
    }

	public static Vector3 CatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float r) {
		return p0 * (-r*t*t*t + 2*r*t*t - r*t) + p1 * ((2-r)*t*t*t + (r-3)*t*t + 1.0f) + p2 * ((r-2)*t*t*t + (3-2*r)*t*t + r*t) + p3 * (r*t*t*t - r*t*t);
	}
	
	void Awake() {
		//Debug.Log("created clip");
		TransformBinding.exposedName = UnityEditor.GUID.Generate ().ToString ();
		if (!created) {
		//if (pathColor==null) {
			pathColor = Random.ColorHSV(0.0f, 1.0f,0.5f, 1.0f, 0.9f, 1.0f);
			created=true;
		}
	}
	void OnEnable() {
		//Debug.Log("enabled");
	}
	void OnDisable() {
		//Debug.Log("disabled");
	}

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
//		Debug.Log("creating playable");
		TL = graph;

		var handle = ScriptPlayable<PathFollowBehaviour>.Create(graph,data);
		data = handle.GetBehaviour();
		//data.TLClip.blendInCurveMode = TimelineClip.BlendCurveMode.Auto;
		//data.TLClip.EvaluateMixIn(0.2);
		return handle;
	}
	
	
}

[System.Serializable]
public class PathFollowBehaviour : PlayableBehaviour
{
	public Transform TransformBinding;
	public List<Vector3> pathPoints = new List<Vector3>();
	public float progress = 0;
	public TimelineClip TLClip;
	public AnimationCurve animcurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

}

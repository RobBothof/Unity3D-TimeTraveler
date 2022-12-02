using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.2f,1.0f,0.2f)]
[TrackClipType(typeof(PathFollow))]
[TrackBindingType(typeof(Transform))]
public class PathFollowTrack : TrackAsset
{
	public string myname;
    public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
    {
		
#if UNITY_EDITOR
        var comp = director.GetGenericBinding(this) as Transform;
        if (comp == null)
            return;
        var so = new UnityEditor.SerializedObject(comp);
        var iter = so.GetIterator();
        while (iter.NextVisible(true))
        {
            if (iter.hasVisibleChildren)
                continue;
            driver.AddFromName<Transform>(comp.gameObject, iter.propertyPath);
        }
#endif
        base.GatherProperties(director, driver);
		    
}
	
	public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
		var director = go.GetComponent<PlayableDirector>();
		var binding = director.GetGenericBinding(this);
		foreach (var c in GetClips())
		{
			PathFollow myAsset = c.asset as PathFollow;
			if (myAsset != null) {
				myAsset.clipName = c.displayName;
				Transform t = binding as Transform;
				graph.GetResolver().SetReferenceValue(myAsset.TransformBinding.exposedName, t);
				myAsset.data.TransformBinding = t;
				myAsset.data.TLClip = c;
			}
		}
		//return base.CreateTrackMixer(graph, go, inputCount);
		return ScriptPlayable<PathFollowMixer>.Create (graph, inputCount);

	}
	
}


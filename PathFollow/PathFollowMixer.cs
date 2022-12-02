using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class PathFollowMixer : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        Transform trackBinding = playerData as Transform;
		int inputCount = playable.GetInputCount ();
		double currentTime = playable.GetGraph().GetRootPlayable(0).GetTime();

		bool activeclip =false;
		double nearestEndTime = Mathf.Infinity;;
		int nearestEndClip = -1;

		double nearestStartTime = Mathf.Infinity;;
		int nearestStartClip = -1;
		
		for (int i = 0; i < inputCount; i++)
        {
            ScriptPlayable<PathFollowBehaviour> clip = (ScriptPlayable<PathFollowBehaviour>)playable.GetInput (i);
            PathFollowBehaviour clipdata = clip.GetBehaviour ();

			clipdata.progress = (float) ((currentTime-clipdata.TLClip.start) / clip.GetDuration());
			
			if ((clipdata.TLClip.end >= currentTime) && (clipdata.TLClip.start <= currentTime)) {
				activeclip=true;
				if (clipdata.pathPoints.Count > 1) {
					float curvepos=(float) ((currentTime-clipdata.TLClip.start)/clip.GetDuration());
					float floatpos=(clipdata.pathPoints.Count-1) * (Mathf.Clamp(clipdata.animcurve.Evaluate(curvepos),0.0f,1.0f));
					int intpos = Mathf.FloorToInt(floatpos);

					if (intpos < clipdata.pathPoints.Count-1) {
						trackBinding.position = Vector3.Lerp(clipdata.pathPoints[intpos], clipdata.pathPoints[intpos+1], floatpos-intpos);
					} else {
						trackBinding.position = clipdata.pathPoints[clipdata.pathPoints.Count-1];
					}
					
				} else {
					if (clipdata.pathPoints.Count > 0) {
						trackBinding.position = clipdata.pathPoints[0];
					}
						
				}
			} else if (clipdata.TLClip.end < currentTime) {
				if (currentTime - clipdata.TLClip.end < nearestEndTime) {
					nearestEndTime=currentTime - clipdata.TLClip.end;
					nearestEndClip = i;
				}
			} else if (clipdata.TLClip.start > currentTime) {
				if (clipdata.TLClip.start - currentTime < nearestStartTime) {
					nearestStartTime=clipdata.TLClip.start-currentTime;
					nearestStartClip = i;
				}
			}
		}
		
		if (!activeclip) {
			if (nearestEndClip!=-1) {
				ScriptPlayable<PathFollowBehaviour> clip = (ScriptPlayable<PathFollowBehaviour>)playable.GetInput (nearestEndClip);
				PathFollowBehaviour clipdata = clip.GetBehaviour ();
				if (clipdata.pathPoints.Count > 0) {
					trackBinding.position = clipdata.pathPoints[clipdata.pathPoints.Count-1];
				}
			} else if (nearestStartClip!=-1) {
				ScriptPlayable<PathFollowBehaviour> clip = (ScriptPlayable<PathFollowBehaviour>)playable.GetInput (nearestStartClip);
				PathFollowBehaviour clipdata = clip.GetBehaviour ();
				if (clipdata.pathPoints.Count > 0) {
					trackBinding.position = clipdata.pathPoints[0];
				}

			}
		}
		
	}
}

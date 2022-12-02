//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using UnityEngine.Playables;
//using UnityEngine.Timeline;
//using UnityEditorInternal;


/*

Time Traveler

Simple but Powerfull tool for animating Transforms through TimeLine Clips.

Unleas the power of Unity's timeline. preview dynamic level-design directly in Edit mode.
Aims to be a a smart, fast and versitile tool, without exposing an endless amount of parameters.

Features extensive multi-editing support, to edit and multiple animation path simultaniously
Ideal for cinematics, shmups, VJ-systems or any project requiring versitale TimeLine based animation.

 */

[CustomEditor(typeof(PathFollow))]
[CanEditMultipleObjects]
public class PathFollowInspector : Editor {
	
	//clipdata
	static PathFollow[] PFClips;

	//control point selection vars
	static Vector2 cp_MouseStart;
	static float cp_ClickTime = 0;
	static int cp_ClickID;
	static float cp_DoubleClickInterval = 0.5f;

	// selection vars
	static Vector2 selectionStartPos;
	static Vector2 selectionEndPos;
	static bool selection;
	static bool[][] preselection;

	//reorderable list vars
	static Vector2 l_MouseStart;
	static bool l_selected;
	static int l_selectedindex=0;
	static int l_clip=0;
	static bool l_dragged;
	static float l_dragoffset =0;
	static Editor thisEditor;
	static bool mouseDownUndo = false;

	//style vars
	GUIStyle boldredStyle;
	GUIStyle inverseStyle;
	GUIStyle ClipLabelStyle;
	GUIStyle ClipLabelRightStyle;
	static GUIStyle cpToolBar;
	static GUIStyle ToolBarTitleStyle;
	static GUIStyle buttonStyle;

	private static GUIStyle ToggleButtonStyleNormal=null;
	private static GUIStyle ToggleButtonStyleToggled=null;

	static Texture2D bgImage;
	Texture2D listSelectTex;
	
	static Texture2D LogoTex;
	static Texture2D MoveIcon;
	static Texture2D MoveIconOn;
	static Texture2D RotateIcon;
	static Texture2D RotateIconOn;
	static Texture2D ScaleIcon;
	static Texture2D ScaleIconOn;
	static Texture2D PointIcon;
	static Texture2D PointIconOn;
	static Texture2D[] toolitems = new Texture2D[4];
	static int selectedtool = 0;

	public Texture2D TriangleTexClosed;
	public Texture2D TriangleTexOpen;
	
	GUIContent[] pivottoolset;
	GUIContent[] globaltoolset;
	GUIContent toolCenterContent;
	GUIContent toolPivotContent;
	GUIContent toolGlobalContent;
	GUIContent toolLocalContent;
	static bool centerMode = true;
	static bool pivotMode = false;
	static bool globalMode = true;
	static bool localMode = false;
	
	
	static bool moveToggle = true;
	static Vector3 closest;

	public static Vector3 CatmullRom(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float r) {
		return p0 * (-r*t*t*t + 2*r*t*t - r*t) + p1 * ((2-r)*t*t*t + (r-3)*t*t + 1.0f) + p2 * ((r-2)*t*t*t + (3-2*r)*t*t + r*t) + p3 * (r*t*t*t - r*t*t);
	}

	void OnEnable () {
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
		SceneView.onSceneGUIDelegate += OnSceneGUI;

		Undo.undoRedoPerformed -= UndoCallback;
		Undo.undoRedoPerformed += UndoCallback;

		//create clipdata holder for each selected Clip
		Object[] PFObjects = targets;
        PFClips = new PathFollow[PFObjects.Length];
        for (int i = 0; i < PFClips.Length; i++) {
            PFClips[i] = PFObjects[i] as PathFollow;
		}

		//create a static link to this script for onscenegui
		thisEditor = this;

		//setyp styling
		initStyles();
		
	}
	void OnDisable() {
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
		Undo.undoRedoPerformed -= UndoCallback;
	}

	void initStyles() {
		//Style setup
		LogoTex = Resources.Load<Texture2D>("TT/logo");
		
		MoveIcon = Resources.Load<Texture2D>("TT/tt-moveicon");
		MoveIconOn = Resources.Load<Texture2D>("TT/tt-moveicon-act");;
		RotateIcon = Resources.Load<Texture2D>("TT/tt-rotateicon");
		RotateIconOn = Resources.Load<Texture2D>("TT/tt-rotateicon-act");
		ScaleIcon = Resources.Load<Texture2D>("TT/tt-scaleicon");
		ScaleIconOn = Resources.Load<Texture2D>("TT/tt-scaleicon-act");
		PointIcon = Resources.Load<Texture2D>("TT/tt-pointicon");
		PointIconOn = Resources.Load<Texture2D>("TT/tt-pointicon-act");
		
		TriangleTexClosed  = Resources.Load<Texture2D>("TT/tt-triangle-closed");
		TriangleTexOpen= Resources.Load<Texture2D>("TT/tt-triangle-open");

		pivottoolset= new GUIContent[1];
		globaltoolset = new GUIContent[1];
		toolCenterContent = new GUIContent(" Center", EditorGUIUtility.FindTexture( "ToolHandleCenter"), "Pivot placed at avage center of selected controlPoints");
		toolPivotContent  = new GUIContent(" Pivot", EditorGUIUtility.FindTexture( "ToolHandlePivot"), "Pivot placed at the controlPoint that is selected first");
		toolGlobalContent = new GUIContent(" Global", EditorGUIUtility.FindTexture( "ToolHandleGlobal"), "Transformation are Applied to selected controlPoints for all Clips as one group");
		toolLocalContent = new GUIContent(" Local", EditorGUIUtility.FindTexture( "ToolHandleLocal"), "Transformation are Applied to selected controlPoints for each individual Clip");
		
		bgImage = new Texture2D(1,1);
		bgImage.SetPixel(0,0, new Color(0.8f, 0.8f, 0.8f, 1.0f));
		bgImage.Apply();

		listSelectTex = new Texture2D(1, 1);
		listSelectTex.SetPixel(0, 0, new Color(0.80f, 0.5f, 0.5f, 1.0f));
		listSelectTex.Apply();

		boldredStyle = new GUIStyle();
		boldredStyle.fontStyle = FontStyle.Bold;
		boldredStyle.normal.textColor = Color.red;

		inverseStyle = new GUIStyle();
		inverseStyle.fontStyle = FontStyle.Bold;
		inverseStyle.normal.textColor = Color.white;
		inverseStyle.normal.background = bgImage;

		ClipLabelStyle = new GUIStyle();
		ClipLabelStyle.fontStyle = FontStyle.Bold;
		ClipLabelStyle.fontSize = 12;
		ClipLabelStyle.alignment = TextAnchor.UpperLeft;
		ClipLabelRightStyle = new GUIStyle();
		ClipLabelRightStyle.fontStyle = FontStyle.Bold;
		ClipLabelRightStyle.fontSize = 12;
		ClipLabelRightStyle.alignment = TextAnchor.UpperRight;


		cpToolBar = new GUIStyle();
		cpToolBar.normal.background=bgImage;
		cpToolBar.normal.textColor=Color.white;
		cpToolBar.margin=new RectOffset(0,0,0,0);

		ToolBarTitleStyle = new GUIStyle();
		ToolBarTitleStyle.fontSize=12;
		ToolBarTitleStyle.fontStyle = FontStyle.Bold;
			
		buttonStyle = new GUIStyle();
		//buttonStyle = GUI.skin.button;
		buttonStyle.fontSize=18;
		//buttonstyle.normal.textColor=Color.black;
		//buttonstyle.active.textColor=Color.white;
		//ToggleButtonStyleNormal = "Button";
		//ToggleButtonStyleToggled = new GUIStyle(ToggleButtonStyleNormal);
		//ToggleButtonStyleToggled.normal.background = ToggleButtonStyleToggled.active.background;
	}
	//rebuild after undo
	static void UndoCallback() {
		for (int p = 0; p < PFClips.Length; p++) {
			PFClips[p].buildSpline();
			PFClips[p].buildPath();
			EditorUtility.SetDirty(PFClips[p]);
		}
		thisEditor.Repaint();
	}
	
    // Search predicate returns true selected".
    private static bool controlPointIsSelected(controlPoint cp)
    {
        return cp.selected;
    }

	// get total amount of controlControlpoints for all selected Clips
	static int countControls() {
		int c=0;
		for (int p = 0; p < PFClips.Length; p++) {
			c+=PFClips[p].controlPoints.Count;
		}
		return c;
	}

	// get total amount of SELECTED controlControlpoints for all selected Clips
	static int countSelectedControls() {
		int c=0;
		for (int p = 0; p < PFClips.Length; p++) {
			for (int i=0; i< PFClips[p].controlPoints.Count;i++) {
				if (PFClips[p].controlPoints[i].selected) c++;
			}
		}
		return c;
	}
	
	// deselect all controlControlpoints for selected Clips
	static void deselectAll() {
		for (int p = 0; p < PFClips.Length; p++) {
			for (int i=0; i< PFClips[p].controlPoints.Count;i++) {
				PFClips[p].controlPoints[i].selected=false;
			}
		}
	}

	// get average position for group selected controlPoint manipulation
	static Vector3 groupHandlePosition() {
		Vector3 avg = Vector3.zero;
		float c = 0;
		for (int p = 0; p < PFClips.Length; p++) {
			for (int i=0; i< PFClips[p].controlPoints.Count;i++) {
				if (PFClips[p].controlPoints[i].selected) {
					c++;
					avg+=PFClips[p].controlPoints[i].pos;
				}
			}
		}
		return avg / c;
	}

	
	static void OnSceneGUI(SceneView sv) {
		
		Event e = Event.current;

		// prevent undo when dragging sliders
		if (e.type == EventType.MouseDown) 	mouseDownUndo = true;
		if (e.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow) mouseDownUndo = false;
		
		// disable default sceneview selection controls
		HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

		//create list of HandleIDs
		int[] controlIDs = new int[countControls()]; 
		int controlCounter=0;
		for (int p = 0; p < PFClips.Length; p++) {
			for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
				controlIDs[controlCounter] = GUIUtility.GetControlID("controlPointhandle".GetHashCode(), FocusType.Passive);
				controlCounter++;
			}
		}

		//groupHandle and translation offset for multiple selected controlpoints
		int selectionCount = countSelectedControls();
		Vector3 offset = Vector3.zero;
		if (selectionCount > 1) {
			Vector3 grouppos = groupHandlePosition();
			offset = Handles.PositionHandle(grouppos, Quaternion.identity) - grouppos;
			if (offset != Vector3.zero) {
				if (mouseDownUndo) {
					Undo.RecordObjects(PFClips, "ControlPoints Move");
					mouseDownUndo=false;
				}
			}

		}

		//position Handle for individual selected controlPoints and do actual translation (group or individual) 
		for (int p = 0; p < PFClips.Length; p++) {
			bool updateSpline=false;
			Vector3 handlepos;

			for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
				//individual position handle and translation
				handlepos = PFClips[p].controlPoints[i].pos; 
				if (selectionCount == 1) {
					if (PFClips[p].controlPoints[i].selected) {
						handlepos = Handles.PositionHandle(handlepos, Quaternion.identity);
						if (handlepos != PFClips[p].controlPoints[i].pos) {
							if (mouseDownUndo) {
								Undo.RecordObject(PFClips[p], "ControlPoint Move");
								mouseDownUndo=false;
							}
							PFClips[p].controlPoints[i].pos=handlepos;
							updateSpline=true;
						}
					}
				}
				//group translation
				if (selectionCount > 1) {
					if (offset != Vector3.zero) {
						if (PFClips[p].controlPoints[i].selected) {
							handlepos+=offset;
							if (handlepos != PFClips[p].controlPoints[i].pos) {
								PFClips[p].controlPoints[i].pos=handlepos;
								updateSpline=true;
							}
						}
					}
				}
			}
			//rebuild if needed
			if (updateSpline) {
				PFClips[p].buildSpline();
				PFClips[p].buildPath();
				EditorUtility.SetDirty(PFClips[p]);
				PFClips[p].TL.Evaluate();
				thisEditor.Repaint();
			}
		}
		//layout controlpoint selection handles
		if (e.type == EventType.Layout) {
			int cc=0;
			for (int p = 0; p < PFClips.Length; p++) {
				for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
					HandleUtility.AddControl(controlIDs[cc], HandleUtility.DistanceToCircle(PFClips[p].controlPoints[i].pos, 2));
					cc++;
				}
			}
		}

		//Draw  controlpoint selection handles and lines
		if (e.type == EventType.Repaint) {
			int cc=0;
			for (int p = 0; p < PFClips.Length; p++) {
				Color beforeColor = Handles.color;

				//Draw SPLINES
				if (PFClips[p].data.pathPoints.Count > 0) {
					Handles.color = PFClips[p].pathColor;
					Handles.DrawPolyLine(PFClips[p].data.pathPoints.ToArray());
					Handles.color=beforeColor;
				}
				Handles.color = PFClips[p].pathColor;
				for(int i = 0; i < PFClips[p].data.pathPoints.Count; i++) {
					Handles.SphereHandleCap(0, PFClips[p].data.pathPoints[i], Quaternion.identity, HandleUtility.GetHandleSize( PFClips[p].data.pathPoints[i])*0.04f, EventType.Repaint);
				}

				Handles.color = beforeColor;

				// Draw ControlPoints
				for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
					if (PFClips[p].controlPoints[i].selected) Handles.color = Color.red;
					Handles.SphereHandleCap(controlIDs[cc], PFClips[p].controlPoints[i].pos, Quaternion.identity, HandleUtility.GetHandleSize( PFClips[p].controlPoints[i].pos)*0.12f, EventType.Repaint);
					Handles.color = beforeColor;
					cc++;
				}

				if (selectedtool==3) {
					if (HandleUtility.DistanceToCircle(closest, 1) < 25) {
						Handles.color = Color.green;
						Handles.SphereHandleCap(100, closest, Quaternion.identity, HandleUtility.GetHandleSize(closest)*0.15f, EventType.Repaint);
						Handles.color = beforeColor;
					}
				}

				//Handles.SphereHandleCap(100, PFClips[p].beforePoint, Quaternion.identity, HandleUtility.GetHandleSize(PFClips[p].beforePoint)*0.15f, EventType.Repaint);
				//Handles.SphereHandleCap(100, PFClips[p].afterPoint, Quaternion.identity, HandleUtility.GetHandleSize(PFClips[p].afterPoint)*0.15f, EventType.Repaint);


				//Handles.color = Color.blue;
				//for(int i = 0; i < PFClips[p].splinePoints.Count; i++) {
				//	Handles.SphereHandleCap(0, PFClips[p].splinePoints[i].pos, Quaternion.identity, HandleUtility.GetHandleSize( PFClips[p].splinePoints[i].pos)*0.06f, EventType.Repaint);
				//}

			}
		}

		//individual ControlPoint selection mousedown event
		if (e.type == EventType.MouseDown) {
			int cc=0;
			for (int p = 0; p < PFClips.Length; p++) {
				for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
					if (HandleUtility.nearestControl == controlIDs[cc] && (Event.current.button == 0)) {
						GUIUtility.hotControl = controlIDs[cc];
						cp_MouseStart = Event.current.mousePosition;
						e.Use();
						if ((e.modifiers == EventModifiers.Shift) || (e.modifiers == EventModifiers.Control)) {
							Undo.RecordObject(PFClips[p], "ControlPoint Selection change");
							if (PFClips[p].controlPoints[i].selected) {
								PFClips[p].controlPoints[i].selected=false;
							} else {
								PFClips[p].controlPoints[i].selected=true;
							}								
						} else {
							if (PFClips[p].controlPoints[i].selected) {
								if (selectionCount ==1) {
									Undo.RecordObject(PFClips[p], "ControlPoint Selection change");
									PFClips[p].controlPoints[i].selected=false;
								} else {
									Undo.RecordObjects(PFClips, "ControlPoints Selection change");
									deselectAll();
									PFClips[p].controlPoints[i].selected=true;
								}
							} else {
								Undo.RecordObjects(PFClips, "ControlPoints Selection change");
								deselectAll();
								PFClips[p].controlPoints[i].selected=true;
							}
						}
						thisEditor.Repaint();

					}
					cc++;
				}
			}
		}
		//individual ControlPoint selection mouseup event // detect doubleclick
		if (e.type == EventType.MouseUp) {
			if (Event.current.button == 0) {
				int cc=0;
				for (int p = 0; p < PFClips.Length; p++) {
					bool doubleClick = false;
					for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
						if (GUIUtility.hotControl == controlIDs[cc] && (Event.current.button == 0)) {
							GUIUtility.hotControl = 0;
							bool thisDoubleClick = false;
							e.Use();
							if (Event.current.mousePosition == cp_MouseStart) {
								thisDoubleClick = (cp_ClickID == controlIDs[cc]) && (Time.realtimeSinceStartup - cp_ClickTime < cp_DoubleClickInterval);
								if (thisDoubleClick) {
									doubleClick=true;
								} else {
									cp_ClickID = controlIDs[cc];
									cp_ClickTime = Time.realtimeSinceStartup;
								}
							}
						}
						cc++;
					}
					//select entire path when doubleclicked
					if (doubleClick) {
						for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
							Undo.RecordObject(PFClips[p], "ControlPoints Selection change");
							PFClips[p].controlPoints[i].selected=true;
						}
						thisEditor.Repaint();
					}
				}
			}
		}

		//group selection mousedown event // cache current selection and start selection region
		if (e.type == EventType.MouseDown) {
			if (Event.current.button == 0) {
				Rect sceneviewRect = new Rect(0, 0, Screen.width, Screen.height);
				if (sceneviewRect.Contains(Event.current.mousePosition)) {
					if ((e.modifiers != EventModifiers.Shift) && (e.modifiers != EventModifiers.Control)) {
						Undo.RecordObjects(PFClips, "ControlPoints Selection change");
						deselectAll();
					}
					selectionStartPos = e.mousePosition;
					selectionEndPos = e.mousePosition;
					selection=true;
					preselection = new bool[PFClips.Length][];
					for (int p = 0; p < PFClips.Length; p++) {
						preselection[p] = new bool[PFClips[p].controlPoints.Count];
						for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
							preselection[p][i] = PFClips[p].controlPoints[i].selected;
						}
					}
					e.Use();
				}
			}
		}

		//group selection mouseup event // end the selection region and select candidates
		if (((e.type == EventType.MouseUp) && (Event.current.button==0)) || (e.type == EventType.MouseLeaveWindow)) {
				if (selection) {
					selection=false;
					Event.current.Use();
					for (int p = 0; p < PFClips.Length; p++) {
						for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
							Rect selectionRect =new Rect(selectionStartPos.x,selectionStartPos.y,selectionEndPos.x-selectionStartPos.x,(selectionEndPos.y-selectionStartPos.y));
							if (selectionRect.Contains(HandleUtility.WorldToGUIPoint(PFClips[p].controlPoints[i].pos),true)) {
								if (e.modifiers == EventModifiers.Shift) 
									PFClips[p].controlPoints[i].selected=true;
								if (e.modifiers == EventModifiers.Control) 
									PFClips[p].controlPoints[i].selected=false;
								if (e.modifiers == EventModifiers.None)
									PFClips[p].controlPoints[i].selected=true;
							}
						}
					}
					thisEditor.Repaint();
				}
		}

		//group selection mousedrag event // set the selection region and show possible new selections
		if (e.type == EventType.MouseDrag) {
			if (Event.current.button == 0) {
				if (selection) {
					selectionEndPos = Event.current.mousePosition;
					if (mouseDownUndo) {
						Undo.RecordObjects(PFClips, "ControlPoints Selection change");
					}
					for (int p = 0; p < PFClips.Length; p++) {
						for(int i = 0; i < PFClips[p].controlPoints.Count; i++) {
							PFClips[p].controlPoints[i].selected = preselection[p][i];
							Rect selectionRect =new Rect(selectionStartPos.x,selectionStartPos.y,selectionEndPos.x-selectionStartPos.x,(selectionEndPos.y-selectionStartPos.y));
							if (selectionRect.Contains(HandleUtility.WorldToGUIPoint(PFClips[p].controlPoints[i].pos),true)) {
								if (e.modifiers == EventModifiers.Shift) 
									PFClips[p].controlPoints[i].selected=true;
								if (e.modifiers == EventModifiers.Control) 
									PFClips[p].controlPoints[i].selected=false;
								if (e.modifiers == EventModifiers.None)
									PFClips[p].controlPoints[i].selected=true;
							}
						}
					}
					thisEditor.Repaint();
					Event.current.Use();
				}
			}
		}


		//KEYBOARD SHORTCUTS
		if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.W)) {
			selectedtool=0;
		}
		if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.E)){
			selectedtool=1;
		}
		if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.R)) {
			selectedtool=2;
		}
		if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.T)){
			selectedtool=3;
		}

		if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.Z)){
			if (centerMode) {
				centerMode=false;
				pivotMode=true;
			} else {
				pivotMode=false;
				centerMode=true;
			}
		}
		if ((e.type == EventType.KeyDown) && (e.keyCode == KeyCode.X)){
			if (globalMode) {
				globalMode=false;
				localMode=true;
			} else {
				localMode=false;
				globalMode=true;
			}
		}
		if (e.type == EventType.MouseMove) {
			HandleUtility.Repaint();
		}
		if (selectedtool==3) {
				//Debug.Log("mousemove");
				for (int p = 0; p < PFClips.Length; p++) {
					Vector3 newclosest = HandleUtility.ClosestPointToPolyLine(PFClips[p].data.pathPoints.ToArray());
					if (closest != newclosest) {
						closest = newclosest;
					}
				}
			//}
		}

		
		Handles.BeginGUI();
		
		if (selectedtool==3) {
			EditorGUIUtility.AddCursorRect(new Rect(0, 0, Screen.width, Screen.height), MouseCursor.ArrowPlus);
			
		}
		// Draw selection Region
		if (selection) {
			Rect selectionRect =new Rect(selectionStartPos.x,selectionStartPos.y,selectionEndPos.x-selectionStartPos.x,selectionEndPos.y-selectionStartPos.y);
			Handles.DrawSolidRectangleWithOutline(selectionRect, new Color(0.5f,1,1,0.1f), Color.white);
		}

		Handles.EndGUI();


		
	}
	

	public override void OnInspectorGUI() {
		
		EditorGUIUtility.labelWidth=110;
		//no undo when dragging sliders
		if (Event.current.type == EventType.MouseDown) {
			mouseDownUndo = true;
		}
		if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow) {
			mouseDownUndo = false;
		}
		
		int oldindent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		//if (Tools.current == Tool.Move) selectedtool=0;
		//if (Tools.current == Tool.Rotate) selectedtool=1;
		//if (Tools.current == Tool.Scale) selectedtool=2;
				
		if (selectedtool==0) {
			toolitems[0] = MoveIconOn;
			toolitems[1] = RotateIcon;
			toolitems[2] = ScaleIcon;
			toolitems[3] = PointIcon;
		}
		if (selectedtool==1) {
			toolitems[0] = MoveIcon;
			toolitems[1] = RotateIconOn;
			toolitems[2] = ScaleIcon;
			toolitems[3] = PointIcon;
		}
		if (selectedtool==2) {
			toolitems[0] = MoveIcon;
			toolitems[1] = RotateIcon;
			toolitems[2] = ScaleIconOn;
			toolitems[3] = PointIcon;
		}
		if (selectedtool==3) {
			toolitems[0] = MoveIcon;
			toolitems[1] = RotateIcon;
			toolitems[2] = ScaleIcon;
			toolitems[3] = PointIconOn;
		}		
		Rect logor = EditorGUILayout.GetControlRect(false,25);
		EditorGUI.DrawRect(logor, Color.black);

		GUI.DrawTexture(new Rect(logor.x,logor.y,230,25), LogoTex,ScaleMode.ScaleAndCrop,false);

		Rect toolbr = EditorGUILayout.GetControlRect(false,71);
		//EditorGUI.DrawRect(new Rect(toolbr.x,toolbr.y,toolbr.width,4), new Color(0.5f,0.5f,0.5f,0.5f));
		//EditorGUI.DrawRect(new Rect(toolbr.x,toolbr.y+280,toolbr.width,4), new Color(0.5f,0.5f,0.5f,0.5f));

		GUI.Label(new Rect(toolbr.x,toolbr.y+10,toolbr.width,20), " Path manipulation:",ToolBarTitleStyle);

		int newtool = GUI.Toolbar(new Rect(toolbr.x,toolbr.y+35,150,26),selectedtool, toolitems);

		float maxbutwidth=Mathf.Min(200,toolbr.width*0.3f);

		if (centerMode) pivottoolset[0] = toolCenterContent;
		if (pivotMode) pivottoolset[0] = toolPivotContent;
		if (globalMode) globaltoolset[0] = toolGlobalContent;
		if (localMode) globaltoolset[0] = toolLocalContent;
		//int newpivot = GUI.Toolbar(new Rect(toolbr.x+190,toolbr.y,toolbr.width-190,16), 3, pivottoolset);
		//int newglobal = GUI.Toolbar(new Rect(toolbr.x+190,toolbr.y+18,toolbr.width-190,16), 3, globaltoolset);
		int newpivot = GUI.Toolbar(new Rect(toolbr.width-maxbutwidth+14,toolbr.y+35,maxbutwidth,16), 3, pivottoolset);
		int newglobal = GUI.Toolbar(new Rect(toolbr.width-maxbutwidth+14,toolbr.y+55,maxbutwidth,16), 3, globaltoolset);
		
		if (newpivot==0) {
			if (centerMode) {
				centerMode=false;
				pivotMode=true;
			} else {
				pivotMode=false;
				centerMode=true;
			}
		}
		if (newglobal==0) {
			if (globalMode) {
				globalMode=false;
				localMode=true;
			} else {
				localMode=false;
				globalMode=true;
			}
		}

		if (newtool != selectedtool) {
			selectedtool = newtool;
			//if (selectedtool == 0) Tools.current = Tool.Move;
			//if (selectedtool == 1) Tools.current = Tool.Rotate;
			//if (selectedtool == 2) Tools.current = Tool.Scale;
		}
		GUILayout.Space(20);

		Rect translr = EditorGUILayout.GetControlRect(false,25);
		EditorGUI.Vector3Field(new Rect(translr.x,translr.y,Mathf.Min(translr.width-maxbutwidth-8,320),20), "", Vector3.zero);
		GUI.Button(new Rect(translr.width-maxbutwidth+14,translr.y,maxbutwidth,16), "Translate");

		Rect rotatr = EditorGUILayout.GetControlRect(false,25);
		EditorGUI.Vector3Field(new Rect(rotatr.x,rotatr.y,Mathf.Min(rotatr.width-maxbutwidth-8,320),20), "", Vector3.zero);
		GUI.Button(new Rect(rotatr.width-maxbutwidth+14,rotatr.y,maxbutwidth,16), "Rotate");

		Rect scaler = EditorGUILayout.GetControlRect(false,25);
		EditorGUI.Vector3Field(new Rect(scaler.x,scaler.y,Mathf.Min(scaler.width-maxbutwidth-8,320),20), "", new Vector3(1.0f,1.0f,1.0f));
		GUI.Button(new Rect(scaler.width-maxbutwidth+14,scaler.y,maxbutwidth,16), "Scale");

		GUILayout.Space(10);

		Rect subdivr = EditorGUILayout.GetControlRect(false,25);
		if (GUI.Button(new Rect(subdivr.x,subdivr.y,110,16), "Subdivide")) {
			for (int p = 0; p < PFClips.Length; p++) {
				List<controlPoint> newcps = PFClips[p].subDivide();

				if (PFClips[p].controlPoints.Count != newcps.Count) {

					Undo.RecordObject(PFClips[p], "Path Subdivide");
					PFClips[p].controlPoints = new List<controlPoint>(newcps.ToArray());
					PFClips[p].buildSpline();
					PFClips[p].buildPath();
					EditorUtility.SetDirty(PFClips[p]);
				}
			}
		}
		GUILayout.Space(6);
		/*
		EditorGUILayout.LabelField(" Clip Overrides:",ToolBarTitleStyle);
		EditorGUILayout.LabelField("(applies to all selected clips)");
		GUILayout.Space(6);

		bool g_loop = EditorGUILayout.Toggle("loop", false);

		float g_tension = EditorGUILayout.Slider("tension:", 0.5f,-2.0f,2.0f);
		float g_resolution = EditorGUILayout.Slider("resolution:", 0.5f,0.0f,1.0f);
		float g_normalization = EditorGUILayout.Slider("normalization:", 0.5f,0.0f,1.0f);
		*/
		GUILayout.Space(20);
		EditorGUILayout.LabelField(" Clips:",ToolBarTitleStyle);
		GUILayout.Space(4);

		for (int p = 0; p < PFClips.Length; p++) {
			bool updateSpline = false;
			GUILayout.Space(2);
			EditorGUI.indentLevel = 0;

			Rect pgr = EditorGUILayout.GetControlRect(false,20);
			pgr.x+=3;
			pgr.width-=3;
			EditorGUI.DrawRect(pgr, Color.black);
			EditorGUI.DrawRect(new Rect(pgr.x+1,pgr.y+1,pgr.width-2,pgr.height-2), PFClips[p].pathColor);
							   //new Color(0.3f,0.3f,0.3f,1.0f));
			EditorGUI.DrawRect(new Rect(pgr.x+1,pgr.y+pgr.height+2,pgr.width*Mathf.Clamp(PFClips[p].data.progress,0.0f,1.0f)-2,2), new Color(0.0f,0.0f,0.0f,1.0f));
			pgr.y+=2;

			Color labelcolor = Color.HSVToRGB(0.0f,0.0f,1.0f-Mathf.Round(PFClips[p].pathColor.grayscale));
			ClipLabelStyle.normal.textColor = labelcolor;
			ClipLabelRightStyle.normal.textColor = labelcolor;
			EditorGUI.LabelField(new Rect(pgr.x+20,pgr.y,pgr.width-20,pgr.height),"clip " , ClipLabelStyle);
			if (PFClips[p].data.TransformBinding == null) {
				EditorGUI.LabelField(pgr,"no Binding set! ",ClipLabelRightStyle);
			} else {		
				EditorGUI.LabelField(pgr, PFClips[p].data.TransformBinding.name + " ",ClipLabelRightStyle);
			}
			//EditorGUI.DrawTextureTransparent(pgr, TriangleTexClosed);
			if (PFClips[p].EditorShowControlPoints) {
			GUI.DrawTexture(new Rect(pgr.x,pgr.y,18,18), TriangleTexOpen,ScaleMode.ScaleAndCrop,true);
			} else {
			GUI.DrawTexture(new Rect(pgr.x,pgr.y,18,18), TriangleTexClosed,ScaleMode.ScaleAndCrop,true);
			}

			if (Event.current.type == EventType.MouseUp) {
				if (Event.current.button == 0) {
					if (pgr.Contains(Event.current.mousePosition)) {
						if (PFClips[p].EditorShowControlPoints) {
							PFClips[p].EditorShowControlPoints=false;
						} else {
							PFClips[p].EditorShowControlPoints=true;
						}
						Event.current.Use();
						Repaint();
					}
				}
			}
			

			//EditorGUILayout.LabelField("", " Path Follow Clip: " + PFClips[p].clipName, inverseStyle);
			//GUILayout.Space(2);

			//if (PFClips[p].data.TransformBinding == null) {
			//	EditorGUILayout.LabelField("Binding:", " not set!",boldredStyle);
			//} else {		
			//	EditorGUILayout.LabelField("Binding:", PFClips[p].data.TransformBinding.name.ToString());
			//}
			//PFClips[p].EditorShowControlPoints = EditorGUILayout.Foldout(PFClips[p].EditorShowControlPoints, "Path settings");
			if (PFClips[p].EditorShowControlPoints) { //controlPoint foldout
				GUILayout.Space(8);

				bool e_loop = EditorGUILayout.Toggle("loop", PFClips[p].loop);
				if (e_loop != PFClips[p].loop) {
					Undo.RecordObject(PFClips[p], "Loop Change");
					PFClips[p].loop = e_loop;
					updateSpline=true;
				}

				bool e_normalized = EditorGUILayout.Toggle("normalize", PFClips[p].normalized);
				if (e_normalized != PFClips[p].normalized) {
					if (mouseDownUndo) {
						Undo.RecordObject(PFClips[p], "Normalization Changed");
						mouseDownUndo=false;
					}
					PFClips[p].normalized = e_normalized;
					if (PFClips[p].normalized) {
						PFClips[p].normalization =1;
					} else {
						PFClips[p].normalization =0;
					}
					updateSpline=true;
				}
				

				float e_tension = EditorGUILayout.Slider("tension:", PFClips[p].tension,-2.0f,2.0f);
				if (e_tension != PFClips[p].tension) {
					if (mouseDownUndo) {
						Undo.RecordObject(PFClips[p], "Tension Change");
						mouseDownUndo=false;
					}
					PFClips[p].tension = e_tension;
					updateSpline=true;
				}
				float e_resolution = EditorGUILayout.Slider("resolution:", PFClips[p].resolution,0.0f,1.0f);
				if (e_resolution != PFClips[p].resolution) {
					if (mouseDownUndo) {
						Undo.RecordObject(PFClips[p], "Tension Change");
						mouseDownUndo=false;
					}
					PFClips[p].resolution = e_resolution;
					updateSpline=true;
				}

				/*
				float e_normalization = EditorGUILayout.Slider("normalization:", PFClips[p].normalization,0.0f,1.0f);
				if (e_normalization != PFClips[p].normalization) {
					if (mouseDownUndo) {
						Undo.RecordObject(PFClips[p], "Normalization Changed");
						mouseDownUndo=false;
					}
					PFClips[p].normalization = e_normalization;
					updateSpline=true;
				}
				*/
				GUILayout.Space(8);

				Rect curver = EditorGUILayout.GetControlRect(false,20);
				//AnimationCurve newcurve = new AnimationCurve(PFClips[p].data.animcurve.keys);
				//newcurve =EditorGUI.CurveField(curver, "animation curve", newcurve, Color.white, new Rect(0,0,1,1));
				PFClips[p].data.animcurve=EditorGUI.CurveField(curver, "animation curve", PFClips[p].data.animcurve, Color.white, new Rect(0,0,1,1));
				//if (newcurve.keys != PFClips[p].data.animcurve.keys) {
				//Undo.RecordObject(PFClips[p], "Animation Curve changed");
				//PFClips[p].data.animcurve = new AnimationCurve(newcurve.keys);
				//}

				
				GUILayout.Space(8);
				Color e_pathColor = EditorGUILayout.ColorField("Path color:", PFClips[p].pathColor);
				if (e_pathColor != PFClips[p].pathColor) {
					Undo.RecordObject(PFClips[p], "Path Color Change");
					PFClips[p].pathColor=e_pathColor;
					updateSpline=true;
				}
						
				GUILayout.Space(8);
				EditorGUILayout.LabelField("controlPoints:");

				float itemHeight = EditorGUIUtility.singleLineHeight+6;
				
				if (PFClips[p].controlPoints.Count==0) {
					Rect zerocr = EditorGUILayout.GetControlRect(false,itemHeight);
					Rect r = new Rect(zerocr.x,zerocr.y+zerocr.height-itemHeight, zerocr.width,itemHeight-2); //rect for drawing
					EditorGUI.DrawRect(r, new Color(0.7f,0.7f,0.7f,1.0f));

				} else {
				
					//create space for reordered list
					Rect cr = EditorGUILayout.GetControlRect(false,itemHeight*PFClips[p].controlPoints.Count);

					// update dragofffset
					if ((Event.current.type == EventType.MouseDrag)&& (Event.current.button==0)) {
						if (l_selected) {
							l_dragoffset = (Event.current.mousePosition - l_MouseStart).y;
							if (l_dragged==false) {
								if (Mathf.Abs(l_dragoffset) > 4) { //drag for at least 4 pixels to activate dragging
									l_dragged=true;
									Repaint();
								}
							} else {
								Repaint();
							}
						}
					}
				
					for (int i=0; i<PFClips[p].controlPoints.Count; i++) {
						float ypos=cr.y + i*itemHeight;
						Rect r = new Rect(cr.x,ypos, cr.width,itemHeight-2); //rect for drawing
						float maxwidth=Mathf.Min(340,r.width*0.8f);
						Rect dragr = new Rect(r.x,r.y,r.width-maxwidth-5,r.height);       //rect for dragging
						if ((Event.current.type == EventType.MouseDown)&& (Event.current.button==1)) {
							GenericMenu cpmenu = new GenericMenu();
							cpmenu.AddItem(new GUIContent("Copy"),false, Debug.Log,"click");
							cpmenu.AddItem(new GUIContent("Paste"),false, Debug.Log,"click");
							cpmenu.AddItem(new GUIContent("Delete"),false, Debug.Log,"click");
							cpmenu.AddSeparator("");
							cpmenu.AddItem(new GUIContent("Insert Above"),false, Debug.Log,"click");
							cpmenu.AddItem(new GUIContent("Insert Below"),false, Debug.Log,"click");
							cpmenu.ShowAsContext();

						}
						// store mousedown for clicking or draggin // selection or reordering
						if ((Event.current.type == EventType.MouseDown)&& (Event.current.button==0)) {
							if (dragr.Contains(Event.current.mousePosition)) {
								l_clip=p;                                       // <---only this clip
								l_selected=true;
								l_selectedindex=i;
								l_MouseStart = Event.current.mousePosition;
								l_dragoffset=0;
								Event.current.Use();
							}
						}
						// check for clicked items and select acoordingly
						if (p==l_clip) {
							if ((Event.current.type == EventType.MouseUp) && (Event.current.button==0)) {
								if (dragr.Contains(Event.current.mousePosition)) {
									if ((l_selected) && (l_selectedindex == i) && (l_dragged==false)) {
										if ((Event.current.shift) || (Event.current.control)) {
											Undo.RecordObject(PFClips[p], "ControlPoint Selection change");
											if (PFClips[p].controlPoints[i].selected) {
												PFClips[p].controlPoints[i].selected=false;
											} else {
												PFClips[p].controlPoints[i].selected=true;
											}								
										} else {
											if (PFClips[p].controlPoints[i].selected) {
												if (countSelectedControls() ==1) {
													Undo.RecordObject(PFClips[p], "ControlPoint Selection change");
													PFClips[p].controlPoints[i].selected=false;
												} else {
													Undo.RecordObjects(PFClips, "ControlPoints Selection change");
													deselectAll();
													PFClips[p].controlPoints[i].selected=true;
												}
											} else {
												Undo.RecordObjects(PFClips, "ControlPoints Selection change");
												deselectAll();
												PFClips[p].controlPoints[i].selected=true;
											}
										}
										l_selected=false;
										l_dragged=false;
										updateSpline=true;
										Event.current.Use();
									}
								}
							}
						}
						//draw item when nothing is dragged // other clips handles drag // or draw/move items that are not being dragged 
						if ((l_selectedindex != i) || (!l_dragged) || (l_clip!=p)) {	
							if ((l_dragged) && (l_clip==p)) {
								if (i<l_selectedindex) {
									if (l_dragoffset+itemHeight*l_selectedindex < itemHeight*(i+1)) {
										r.y += Mathf.Min(itemHeight,-(l_dragoffset+itemHeight*(l_selectedindex-i-1)));
									}
								} else {
									if (l_dragoffset+itemHeight*l_selectedindex > itemHeight*(i-1)) {
										r.y += Mathf.Max(-itemHeight,(-l_dragoffset-itemHeight*(l_selectedindex-i+1)));
									}
								}
							}
							if (PFClips[p].controlPoints[i].selected) {
								EditorGUI.DrawRect(r, new Color(0.8f,0.55f,0.55f,1.0f));
							} else {
								EditorGUI.DrawRect(r, new Color(0.7f,0.7f,0.7f,1.0f));
							}
							
							EditorGUI.LabelField(new Rect(r.x+2,r.y,30,r.height-5), "=");
							Vector3 cppos = EditorGUI.Vector3Field(new Rect(r.width-maxwidth+8,r.y+2,maxwidth,itemHeight-6), GUIContent.none, PFClips[p].controlPoints[i].pos);
							if (cppos != PFClips[p].controlPoints[i].pos) {
								Undo.RecordObjects(PFClips, "ControlPoints Move");
								PFClips[p].controlPoints[i].pos=cppos;
								updateSpline=true;
							}
						
						}
					}
					// draw dragged item on top of the others
					// and reorder when released
					if (l_clip == p) {
						for (int i=0; i<PFClips[p].controlPoints.Count; i++) {
							if ((l_selectedindex == i) && (l_dragged==true)) {

								float ypos=cr.y + i*itemHeight;
								ypos = Mathf.Min(cr.y+cr.height-itemHeight,Mathf.Max(cr.y,ypos+l_dragoffset));

								Rect r = new Rect(15,ypos, cr.width,itemHeight-2); //rect for drawing
								float maxwidth=Mathf.Min(340,r.width*0.8f);

						
								if ((Event.current.type == EventType.MouseUp)&& (Event.current.button==0)) {
									l_selected=false;
									l_dragged=false;
									int placetodrop = Mathf.RoundToInt(Mathf.Max(0,Mathf.Min((PFClips[p].controlPoints.Count-1),l_selectedindex + (l_dragoffset / itemHeight))));
									Undo.RecordObject(PFClips[p], "ControlPoints Reorder");

									controlPoint cpp = PFClips[p].controlPoints[l_selectedindex];
									PFClips[p].controlPoints.Remove(cpp);
									PFClips[p].controlPoints.Insert(placetodrop, cpp);

									updateSpline=true;
									Event.current.Use();
								}

								if (PFClips[p].controlPoints[i].selected) {
									EditorGUI.DrawRect(r, new Color(0.8f,0.55f,0.55f,1.0f));
								} else {
									EditorGUI.DrawRect(r, new Color(0.7f,0.7f,0.7f,1.0f));
								}
								EditorGUI.LabelField(new Rect(r.x+2,r.y,30,r.height-5), "=");
								EditorGUI.Vector3Field(new Rect(r.width-maxwidth+8,r.y+2,maxwidth,itemHeight-6), GUIContent.none, PFClips[p].controlPoints[i].pos);

								//EditorGUI.Vector3Field(new Rect(r.x+110,r.y+2,r.width-112,itemHeight), GUIContent.none, PFClips[p].controlPoints[i].pos);
							}
						}
					}
				}	
				GUILayout.Space(5);
				Rect br = EditorGUILayout.GetControlRect(false,20);

				
				if(	GUI.Button(new Rect(br.width-50,br.y,60,br.height), "Delete")) {
					Undo.RecordObject(PFClips[p], "ControlPoints Remove");
					PFClips[p].controlPoints.RemoveAll(controlPointIsSelected);
					updateSpline=true;
				}
				if(	GUI.Button(new Rect(br.width-115,br.y,60,br.height), "Add")) {
					Undo.RecordObject(PFClips[p], "ControlPoints Add");
					PFClips[p].controlPoints.Add(new controlPoint(new Vector3(0,0,0),false));
					updateSpline=true;
				}
				if(	GUI.Button(new Rect(br.width-180,br.y,60,br.height), "Insert " + '\u25BC')) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("At Begin"),false, Debug.Log,"click");
					menu.AddItem(new GUIContent("At Selection"),false, Debug.Log,"click");
					menu.AddItem(new GUIContent("At End"),false, Debug.Log,"click");
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Before"),false, Debug.Log,"click");
					menu.AddItem(new GUIContent("After"),false, Debug.Log,"click");
					menu.ShowAsContext();
				}
				if(	GUI.Button(new Rect(br.width-245,br.y,60,br.height), "Reverse")) {
					Undo.RecordObject(PFClips[p], "ControlPoints Reverse");
					//PFClips[p].controlPoints.RemoveAll(controlPointIsSelected);
					updateSpline=true;
				}

			}
			

			EditorGUI.indentLevel = 0;
			GUILayout.Space(5);
			EditorGUI.indentLevel = oldindent;
			
			if (updateSpline) {
				PFClips[p].buildSpline();
				PFClips[p].buildPath();
				EditorUtility.SetDirty(PFClips[p]);
				//PFClips[p].TL.Evaluate();
			}
		}
		EditorGUIUtility.labelWidth=0;

		//DrawDefaultInspector();
	}
}



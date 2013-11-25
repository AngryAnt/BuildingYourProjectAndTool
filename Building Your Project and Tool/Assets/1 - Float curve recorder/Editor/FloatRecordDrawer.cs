using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;


[CustomPropertyDrawer (typeof (float))]
public class FloatRecordDrawer : PropertyDrawer
{
	// TODO: Need to properly handle array elements


	class TrackedFloatProperty
	{
		const int kMaxSamples = 100;


		int m_ObjectID;
		string m_Name;

		AnimationCurve m_Curve;
		Rect m_Range;
		float m_TrackMin, m_TrackMax;
		DateTime m_StartTime;


		public TrackedFloatProperty (SerializedProperty property)
		{
			m_ObjectID = property.serializedObject.targetObject.GetInstanceID ();
			m_Name = property.name;

			m_Curve = new AnimationCurve ();
			m_Range = new Rect (0.0f, 0.0f, 0.0f, 0.0f);
			m_TrackMin = Mathf.Infinity;
			m_TrackMax = Mathf.NegativeInfinity;
			m_StartTime = DateTime.Now;

			Update (property);
		}


		public AnimationCurve Curve
		{
			get
			{
				return m_Curve;
			}
		}


		public Rect CurveRange
		{
			get
			{
				return m_Range;
			}
		}


		public void Update (SerializedProperty property)
		{
			if (!this.Equals (property))
			{
				return;
			}

			float
				time = (DateTime.Now - m_StartTime).Seconds,
				value = property.floatValue;

			m_TrackMin = Mathf.Min (m_TrackMin, value);
			m_TrackMax = Mathf.Max (m_TrackMax, value);

			m_Curve.AddKey (time, value);

			if (m_Curve.keys.Length > kMaxSamples)
			{
				m_Curve.RemoveKey (0);
			}

			m_Range = new Rect (m_Curve[0].time, m_TrackMin, time, m_TrackMax - m_TrackMin);
		}


		public override bool Equals (object other)
		{
			TrackedFloatProperty otherTrackedProperty = other as TrackedFloatProperty;
			SerializedProperty otherProperty = other as SerializedProperty;

			if (otherTrackedProperty != null)
			{
				return
					otherTrackedProperty.m_ObjectID.Equals (m_ObjectID) &&
					otherTrackedProperty.m_Name.Equals (m_Name);
			}
			else if (otherProperty != null)
			{
				return
					otherProperty.serializedObject.targetObject.GetInstanceID ().Equals (m_ObjectID) &&
					otherProperty.name.Equals (m_Name);
			}

			return false;
		}


		public override int GetHashCode ()
		{
			return m_ObjectID.GetHashCode () & m_Name.GetHashCode ();
		}
	}


	static List<TrackedFloatProperty> s_TrackedProperties = new List<TrackedFloatProperty> ();


	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		if (!Application.isPlaying || property.serializedObject.isEditingMultipleObjects)
		{
			OnDefaultGUI (position, property, label);
		}
		else if (Tracked (property))
		{
			OnTrackedGUI (position, property, label);
		}
		else
		{
			OnUntrackedGUI (position, property, label);
		}
	}


	void OnDefaultGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty (position, label, property);
			property.floatValue = EditorGUI.FloatField (position, label, property.floatValue);
		EditorGUI.EndProperty ();
	}


	void OnUntrackedGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		Rect
			addButtonRect = new Rect (
				position.x + position.width - position.height,
				position.y,
				position.height,
				position.height
			),
			fieldRect = new Rect (
				position.x,
				position.y,
				position.width - position.height,
				position.height
			);

		EditorGUI.BeginProperty (fieldRect, label, property);
			property.floatValue = EditorGUI.FloatField (fieldRect, label, property.floatValue);
		EditorGUI.EndProperty ();

		if (GUI.Toggle (addButtonRect, false, GUIContent.none))
		{
			Add (property);
		}
	}


	void OnTrackedGUI (Rect position, SerializedProperty property, GUIContent label)
	{
		const float kFieldWidth = 50.0f;

		float curveWidth = position.width * 0.47f;

		Rect
			addButtonRect = new Rect (
				position.x + position.width - position.height,
				position.y,
				position.height,
				position.height
			),
			labelRect = new Rect (
				position.x,
				position.y,
				position.width - position.height,
				position.height
			),
			fieldRect = new Rect (
				position.x + position.width - curveWidth - kFieldWidth - position.height,
				position.y,
				kFieldWidth,
				position.height
			),
			curveRect = new Rect (
				position.x + position.width - curveWidth - position.height,
				position.y,
				curveWidth,
				position.height
			);

		TrackedFloatProperty tracker = GetTracker (property);

		EditorGUI.BeginProperty (position, label, property);
			EditorGUI.PrefixLabel (labelRect, GUIUtility.GetControlID (FocusType.Passive) + 1, label);
			property.floatValue = EditorGUI.FloatField (fieldRect, property.floatValue);
			EditorGUI.CurveField (curveRect, tracker.Curve, Color.yellow, tracker.CurveRange);
		EditorGUI.EndProperty ();

		if (Event.current.type == EventType.Repaint)
		{
			tracker.Update (property);
		}

		if (!GUI.Toggle (addButtonRect, true, GUIContent.none))
		{
			Remove (property);
		}
	}


	int PropertyObjectID (SerializedProperty property)
	{
		return property.serializedObject.targetObject.GetInstanceID ();
	}


	TrackedFloatProperty GetTracker (SerializedProperty property)
	{
		for (int i = 0; i < s_TrackedProperties.Count; ++i)
		{
			if (s_TrackedProperties[i].Equals (property))
			{
				return s_TrackedProperties[i];
			}
		}

		return null;
	}


	bool Tracked (SerializedProperty property)
	{
		return GetTracker (property) != null;
	}


	void Add (SerializedProperty property)
	{
		if (!Tracked (property))
		{
			s_TrackedProperties.Add (new TrackedFloatProperty (property));
		}
	}


	void Remove (SerializedProperty property)
	{
		for (int i = 0; i < s_TrackedProperties.Count;)
		{
			if (s_TrackedProperties[i].Equals (property))
			{
				s_TrackedProperties.RemoveAt (i);
			}
			else
			{
				++i;
			}
		}
	}
}

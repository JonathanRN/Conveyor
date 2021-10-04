using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Jroynoel.Editor
{

	/// <summary>
	/// Adapted from https://unitylist.com/p/10tm/Unity-Editor-Polymorphic-Reorderable-List
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Conveyor))]
	public class ConveyorEditor : UnityEditor.Editor
	{
		private static readonly Color ProSkinTextColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
		private static readonly Color PersonalSkinTextColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);

		private static readonly Color ProSkinSelectionBgColor = new Color(44.0f / 255.0f, 93.0f / 255.0f, 135.0f / 255.0f, 1.0f);
		private static readonly Color PersonalSkinSelectionBgColor = new Color(58.0f / 255.0f, 114.0f / 255.0f, 176.0f / 255.0f, 1.0f);

		private const float AdditionalSpaceMultiplier = 1.0f;

		private const float HeightHeader = 20.0f;
		private const float MarginReorderIcon = 20.0f;
		private const float ShrinkHeaderWidth = 15.0f;
		private const float XShiftHeaders = 15.0f;

		private GUIStyle headersStyle;
		private ReorderableList reordList;

		#region Editor Methods

		private void OnEnable()
		{
			headersStyle = new GUIStyle();
			headersStyle.alignment = TextAnchor.MiddleLeft;
			headersStyle.normal.textColor = EditorGUIUtility.isProSkin ? ProSkinTextColor : PersonalSkinTextColor;
			headersStyle.fontStyle = FontStyle.Bold;

			reordList = new ReorderableList(serializedObject, serializedObject.FindProperty("Steps"), true, true, true, true);
			reordList.drawHeaderCallback += OnDrawReorderListHeader;
			reordList.drawElementCallback += OnDrawReorderListElement;
			reordList.drawElementBackgroundCallback += OnDrawReorderListBg;
			reordList.elementHeightCallback += OnReorderListElementHeight;
			reordList.onAddDropdownCallback += OnReorderListAddDropdown;
		}

		private void OnDisable()
		{
			reordList.drawElementCallback -= OnDrawReorderListElement;
			reordList.elementHeightCallback -= OnReorderListElementHeight;
			reordList.drawElementBackgroundCallback -= OnDrawReorderListBg;
			reordList.drawHeaderCallback -= OnDrawReorderListHeader;
			reordList.onAddDropdownCallback -= OnReorderListAddDropdown;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.Space(10);

			serializedObject.Update();

			reordList.DoLayoutList();

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region ReorderableList Callbacks

		private void OnDrawReorderListHeader(Rect rect)
		{
			EditorGUI.LabelField(rect, "Steps");
		}

		private void OnDrawReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
		{
			int length = reordList.serializedProperty.arraySize;

			if (length <= 0)
				return;

			SerializedProperty iteratorProp = reordList.serializedProperty.GetArrayElementAtIndex(index);
			string name = ((Conveyor)iteratorProp.serializedObject.targetObject).Steps[index].ToString();

			Rect labelfoldRect = rect;
			labelfoldRect.height = HeightHeader;
			labelfoldRect.x += XShiftHeaders;
			labelfoldRect.width -= ShrinkHeaderWidth;

			iteratorProp.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(labelfoldRect, iteratorProp.isExpanded, name);

			if (iteratorProp.isExpanded)
			{
				++EditorGUI.indentLevel;

				int i = 0;
				LoopNext(iteratorProp, false, () =>
				{
					float multiplier = i == 0 ? AdditionalSpaceMultiplier : 1.0f;
					rect.y += GetDefaultSpaceBetweenElements() * multiplier;
					rect.height = EditorGUIUtility.singleLineHeight;

					EditorGUI.PropertyField(rect, iteratorProp, true);

					++i;
				});

				--EditorGUI.indentLevel;
			}

			EditorGUI.EndFoldoutHeaderGroup();
		}

		private void OnDrawReorderListBg(Rect rect, int index, bool isActive, bool isFocused)
		{
			if (!isFocused || !isActive)
				return;

			float height = OnReorderListElementHeight(index);

			SerializedProperty prop = reordList.serializedProperty.GetArrayElementAtIndex(index);

			// remove a bit of the line that goes beyond the header label
			if (!prop.isExpanded)
				height -= EditorGUIUtility.standardVerticalSpacing;

			Rect copyRect = rect;
			copyRect.width = MarginReorderIcon;
			copyRect.height = height;

			// draw two rects indepently to avoid overlapping the header label
			Color color = EditorGUIUtility.isProSkin ? ProSkinSelectionBgColor : PersonalSkinSelectionBgColor;
			EditorGUI.DrawRect(copyRect, color);

			float offset = 2.0f;
			rect.x += MarginReorderIcon;
			rect.width -= MarginReorderIcon + offset;

			rect.height = height - HeightHeader + offset;
			rect.y += HeightHeader - offset;

			EditorGUI.DrawRect(rect, color);
		}

		private float OnReorderListElementHeight(int index)
		{
			int length = reordList.serializedProperty.arraySize;

			if (length <= 0)
				return 0.0f;

			SerializedProperty iteratorProp = reordList.serializedProperty.GetArrayElementAtIndex(index);

			float height = GetDefaultSpaceBetweenElements();

			if (!iteratorProp.isExpanded)
				return height;

			int i = 0;
			LoopNext(iteratorProp, false, () =>
			{
				float multiplier = i == 0 ? AdditionalSpaceMultiplier : 1.0f;
				height += GetDefaultSpaceBetweenElements() * multiplier;
				++i;
			});

			return height;
		}

		private void OnReorderListAddDropdown(Rect buttonRect, ReorderableList list)
		{
			GenericMenu menu = new GenericMenu();
			List<Type> showTypes = GetNonAbstractTypesSubclassOf<ConveyorStep>();

			for (int i = 0; i < showTypes.Count; ++i)
			{
				Type type = showTypes[i];
				string actionName = showTypes[i].Name;

				// Uncomment for single element restriction
				//bool alreadyHasIt = DoesReordListHaveElementOfType(actionName);
				//if (alreadyHasIt)
					//continue;

				InsertSpaceBeforeCaps(ref actionName);
				menu.AddItem(new GUIContent(actionName), false, OnAddItemFromDropdown, type);
			}

			menu.ShowAsContext();
		}

		private void OnAddItemFromDropdown(object obj)
		{
			Type settingsType = (Type)obj;

			int last = reordList.serializedProperty.arraySize;
			reordList.serializedProperty.InsertArrayElementAtIndex(last);

			SerializedProperty lastProp = reordList.serializedProperty.GetArrayElementAtIndex(last);
			lastProp.managedReferenceValue = Activator.CreateInstance(settingsType);

			serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Helper Methods

		private float GetDefaultSpaceBetweenElements()
		{
			return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}

		private void InsertSpaceBeforeCaps(ref string theString)
		{
			for (int i = 0; i < theString.Length; ++i)
			{
				char currChar = theString[i];

				if (char.IsUpper(currChar))
				{
					theString = theString.Insert(i, " ");
					++i;
				}
			}
		}

		private List<Type> GetNonAbstractTypesSubclassOf<T>(bool sorted = true) where T : class
		{
			Type parentType = typeof(T);
			Assembly assembly = Assembly.GetAssembly(parentType);

			List<Type> types = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(parentType)).ToList();

			if (sorted)
				types.Sort(CompareTypesNames);

			return types;
		}

		private int CompareTypesNames(Type a, Type b)
		{
			return a.Name.CompareTo(b.Name);
		}

		private bool DoesReordListHaveElementOfType(string type)
		{
			for (int i = 0; i < reordList.serializedProperty.arraySize; ++i)
			{
				// this works but feels ugly. Type in the array element looks like "managedReference<actualStringType>"
				if (reordList.serializedProperty.GetArrayElementAtIndex(i).type.Contains(type))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Taken from https://forum.unity.com/threads/serializedproperty-nextvisible-doesnt-work-with-hideininspector.937367/
		/// </summary>
		delegate FieldInfo FieldInfoGetter(SerializedProperty p, out Type t);
		public void LoopNext(SerializedProperty propertyAll, bool enterChildren, Action onLoop)
		{
#if UNITY_2019_3_OR_NEWER
			MethodInfo fieldInfoGetterMethod = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility").GetMethod("GetFieldInfoAndStaticTypeFromProperty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#else
		MethodInfo fieldInfoGetterMethod = typeof(Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility").GetMethod("GetFieldInfoFromProperty", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
#endif
			FieldInfoGetter fieldInfoGetter = (FieldInfoGetter)Delegate.CreateDelegate(typeof(FieldInfoGetter), fieldInfoGetterMethod);

			SerializedProperty propertyVisible = serializedObject.GetIterator();

			if (propertyAll.Next(true))
			{
				bool iteratingVisible = propertyVisible.NextVisible(true);
				do
				{
					bool isVisible = iteratingVisible && SerializedProperty.EqualContents(propertyAll, propertyVisible);
					if (isVisible)
					{
						iteratingVisible = propertyVisible.NextVisible(enterChildren);
					}
					else
					{
						Type propfieldType = null;

						// Internal Unity variables don't seem to have a FieldInfo but when SerializedProperty.type is "Array", we must consider it
						// visible to avoid false negatives because even though "Array" type doesn't have a FieldInfo, it can be a visible array property
						isVisible = propertyAll.type == "Array" || fieldInfoGetter(propertyAll, out propfieldType) != null;
						if (propertyAll.propertyType != SerializedPropertyType.ManagedReference && isVisible)
						{
							onLoop?.Invoke();
						}
					}

				} while (propertyAll.Next(enterChildren));
			}
		}
		#endregion
	}
}
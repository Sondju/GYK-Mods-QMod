using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AlchemyResearch
{
  public class Logg
  {
    private static string path = "./QMods/AlchemyResearch/Mod Output.txt";

    public static void Log(string Text)
    {
      if (!File.Exists(Logg.path))
      {
        using (StreamWriter text = File.CreateText(Logg.path))
          text.WriteLine(Text);
      }
      else
      {
        using (StreamWriter streamWriter = File.AppendText(Logg.path))
          streamWriter.WriteLine(Text);
      }
    }

    public static void LogTrace() => Logg.Log("Trace: " + Environment.StackTrace.ToString());

    public static void LogRectTransform(RectTransform Transform)
    {
      Logg.Log("RectTransform " + Transform.name + ".sizeDelta: " + Transform.sizeDelta.ToString());
      Logg.Log("RectTransform " + Transform.name + ".anchorMin: " + Transform.anchorMin.ToString());
      Logg.Log("RectTransform " + Transform.name + ".anchorMax: " + Transform.anchorMax.ToString());
      Logg.Log("RectTransform " + Transform.name + ".anchoredPosition: " + Transform.anchoredPosition.ToString());
      Logg.Log("RectTransform " + Transform.name + ".anchoredPosition3D: " + Transform.anchoredPosition3D.ToString());
      Logg.Log("RectTransform " + Transform.name + ".localPosition: " + Transform.localPosition.ToString());
      Logg.Log("RectTransform " + Transform.name + ".pivot: " + Transform.pivot.ToString());
      Logg.Log("RectTransform " + Transform.name + ".localScale: " + Transform.localScale.ToString());
      Logg.Log("RectTransform " + Transform.name + "-Parent: " + Transform.parent.name);
    }

    public static void LogTransform(GameObject Object) => Logg.LogTransform(Object.transform);

    public static void LogTransform(Transform Transform)
    {
      Logg.Log("Transform " + Transform.name + ".position: " + Transform.position.ToString());
      Logg.Log("Transform " + Transform.name + ".rotation: " + Transform.rotation.eulerAngles.ToString());
      Logg.Log("Transform " + Transform.name + ".localScale: " + Transform.localScale.ToString());
      Logg.Log("Transform " + Transform.name + ".localPosition: " + Transform.localPosition.ToString());
      Logg.Log("Transform " + Transform.name + ".localRotation: " + Transform.localRotation.eulerAngles.ToString());
      bool activeSelf;
      if ((bool) (UnityEngine.Object) Transform.gameObject)
      {
        string[] strArray = new string[6]
        {
          "Transform ",
          Transform.name,
          "-GameObject: ",
          ((object) Transform.gameObject)?.ToString(),
          " - active: ",
          null
        };
        activeSelf = Transform.gameObject.activeSelf;
        strArray[5] = activeSelf.ToString();
        Logg.Log(string.Concat(strArray));
      }
      else
        Logg.Log("Transform " + Transform.name + "-GameObject: <none>");
      if ((bool) (UnityEngine.Object) Transform.gameObject)
      {
        string[] strArray = new string[6]
        {
          "Transform ",
          Transform.name,
          "-Parent: ",
          ((object) Transform.parent.gameObject)?.ToString(),
          " - active: ",
          null
        };
        activeSelf = Transform.parent.gameObject.activeSelf;
        strArray[5] = activeSelf.ToString();
        Logg.Log(string.Concat(strArray));
      }
      else
        Logg.Log("Transform " + Transform.name + "-Parent: <none>");
    }

    public static void LogClear()
    {
      using (File.CreateText(Logg.path))
        ;
    }

    public static string GetFullPath(Transform Transform, Transform Root = null)
    {
      string fullPath = Transform.name;
      for (Transform parent = Transform.parent; (UnityEngine.Object) parent != (UnityEngine.Object) null && (!((UnityEngine.Object) Root != (UnityEngine.Object) null) || !((UnityEngine.Object) parent == (UnityEngine.Object) Root)); parent = parent.parent)
        fullPath = parent.name + "/" + fullPath;
      return fullPath;
    }

    public static void GameObjectInfo(
      MonoBehaviour MonoBehaviour,
      bool ShowComponents = true,
      bool ShowChildren = true,
      bool ShowParents = false,
      int Indentation = 0,
      Transform Root = null)
    {
      Logg.GameObjectInfo(MonoBehaviour.gameObject, ShowComponents, ShowChildren, ShowParents, Indentation);
    }

    public static void GameObjectInfo(
      GameObject GameObject,
      bool ShowComponents = true,
      bool ShowChildren = true,
      bool ShowParents = false,
      int Indentation = 0)
    {
      bool activeSelf;
      if ((UnityEngine.Object) GameObject.transform.parent != (UnityEngine.Object) null)
      {
        string[] strArray = new string[9];
        strArray[0] = new string(' ', Indentation);
        strArray[1] = "- ";
        strArray[2] = GameObject.name;
        strArray[3] = " | active: ";
        activeSelf = GameObject.activeSelf;
        strArray[4] = activeSelf.ToString();
        strArray[5] = " | Parent: ";
        strArray[6] = ((object) GameObject.transform.parent)?.ToString();
        strArray[7] = " | Full Path: ";
        strArray[8] = Logg.GetFullPath(GameObject.transform);
        Logg.Log(string.Concat(strArray));
      }
      else
      {
        string[] strArray = new string[6]
        {
          new string(' ', Indentation),
          "- ",
          GameObject.name,
          " | active: ",
          null,
          null
        };
        activeSelf = GameObject.activeSelf;
        strArray[4] = activeSelf.ToString();
        strArray[5] = " | Parent: <none>";
        Logg.Log(string.Concat(strArray));
      }
      Indentation += 2;
      if (ShowComponents)
      {
        foreach (object component in GameObject.GetComponents<UnityEngine.Object>())
          Logg.Log(new string(' ', Indentation) + "o " + component.ToString());
      }
      if (ShowChildren)
      {
        for (int index = 0; index < GameObject.transform.childCount; ++index)
          Logg.Log(new string(' ', Indentation) + "C " + ((object) GameObject.transform.GetChild(index)).ToString());
        for (int index = 0; index < GameObject.transform.childCount; ++index)
          Logg.GameObjectInfo(GameObject.transform.GetChild(index).gameObject, ShowComponents, ShowChildren, ShowParents, Indentation);
      }
      if (!ShowParents || !((UnityEngine.Object) GameObject.transform.parent != (UnityEngine.Object) null) || !(bool) (UnityEngine.Object) GameObject.transform.parent.gameObject)
        return;
      string[] strArray1 = new string[5]
      {
        GameObject.name,
        " Parent: ",
        GameObject.transform.parent.gameObject.name,
        " | active: ",
        null
      };
      activeSelf = GameObject.transform.parent.gameObject.activeSelf;
      strArray1[4] = activeSelf.ToString();
      Logg.Log(string.Concat(strArray1));
      Logg.GameObjectInfo(GameObject.transform.parent.gameObject, ShowComponents, ShowChildren, ShowParents, Indentation);
    }

    public static void LogObject(object Object)
    {
      string empty1 = string.Empty;
      string empty2 = string.Empty;
      Logg.Log("Object: " + Object?.ToString());
      FieldInfo[] fields = Object.GetType().GetFields(AccessTools.all);
      for (int index = 0; index < fields.Length; ++index)
      {
        string str1 = string.Empty;
        string str2 = string.Empty;
        int num;
        if (fields[index].GetValue(Object) is Array array)
        {
          num = array.Length;
          str1 = " | Array Length: " + num.ToString();
        }
        if (fields[index].GetValue(Object) is List<object> objectList)
        {
          num = objectList.Count;
          str2 = " | List Count: " + num.ToString();
        }
        Logg.Log(" - Field: " + fields[index].Name + " | Value: " + fields[index].GetValue(Object)?.ToString() + str1 + str2);
      }
    }

    public static void LogComponent(UnityEngine.Object Component)
    {
      Logg.Log("Component: " + ((object) Component)?.ToString());
      FieldInfo[] fields = ((object) Component).GetType().GetFields(AccessTools.all);
      for (int index = 0; index < fields.Length; ++index)
        Logg.Log(" - Field: " + fields[index].Name + " | Value: " + fields[index].GetValue((object) Component)?.ToString());
    }
  }
}

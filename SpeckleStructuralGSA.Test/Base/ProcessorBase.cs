﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public abstract class ProcessorBase
  {
    //protected Dictionary<Type, List<Type>> TypePrerequisites = new Dictionary<Type, List<Type>>();

    //Doesn't seem to need this:
    //protected List<KeyValuePair<Type, List<Type>>> TypeCastPriority = new List<KeyValuePair<Type, List<Type>>>();
    protected string TestDataDirectory;

    protected GSAProxy GSAInterfacer;
    protected GSACache GSACache;

    //This should match the private member in GSAInterfacer
    protected const string SID_TAG = "speckle_app_id";

    protected ProcessorBase(string directory)
    {
      TestDataDirectory = directory;
    }

    public List<string> GetKeywords(GSATargetLayer layer)
    {
      // Grab GSA interface and attribute type
      var attributeType = typeof(GSAObject);
      var interfaceType = typeof(IGSASpeckleContainer);

      // Grab all GSA related object
      var ass = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "SpeckleStructuralGSA");
      var objTypes = ass.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t != interfaceType).ToList();

      var TypePrerequisites = new Dictionary<Type, List<Type>>();

      var keywords = new List<string>();

      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Analysis) && !(bool)t.GetAttribute("AnalysisLayer", attributeType)) continue;

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Design) && !(bool)t.GetAttribute("DesignLayer", attributeType)) continue;

        var typeKeyword = t.GetGSAKeyword();
        if (!keywords.Contains(typeKeyword))
        {
          keywords.Add(typeKeyword);
        }
        var subtypeKeywords = t.GetSubGSAKeyword();
        if (subtypeKeywords.Count() > 0)
        {
          for (int i = 0; i < subtypeKeywords.Count(); i++)
          {
            if (!keywords.Contains(subtypeKeywords[i]))
            {
              keywords.Add(subtypeKeywords[i]);
            }
          }
        }
      }

      return keywords;
    }

    /*
    protected void ProcessDeserialiseReturnObject(object deserialiseReturnObject, out string keyword, out int index, out string gwa, out GwaSetCommandType gwaSetCommandType)
    {
      index = 0;
      keyword = "";
      gwa = "";
      gwaSetCommandType = GwaSetCommandType.Set;

      if (!(deserialiseReturnObject is string))
      {
        return;
      }

      var fullGwa = (string)deserialiseReturnObject;

      var pieces = fullGwa.ListSplit("\t").ToList();
      if (pieces.Count() < 2)
      {
        return;
      }

      if (pieces[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
      {
        gwaSetCommandType = GwaSetCommandType.SetAt;
        int.TryParse(pieces[1], out index);
        pieces.Remove(pieces[1]);
        pieces.Remove(pieces[0]);
      }
      else if (pieces[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        gwaSetCommandType = GwaSetCommandType.Set;
        pieces.Remove(pieces[0]);
        int.TryParse(pieces[1], out index);
      }

      gwa = string.Join("\t", pieces);
      gwa.ExtractKeywordApplicationId(out keyword, out var foundIndex, out var sid, out var gwaWithoutSet, out var gwaSetCommandType);
      

      return;
    }
    */
  }
}

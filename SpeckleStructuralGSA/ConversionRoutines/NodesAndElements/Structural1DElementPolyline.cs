﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  //TO DO: check why everything except GSA1DElement is needed as read prerequisites 
  [GSAObject("MEMB.7", new string[] { }, "elements", true, false, new Type[] { typeof(GSA1DElement), typeof(GSA1DLoadAnalysisLayer), typeof(GSA1DElementResult), typeof(GSAAssembly), typeof(GSAConstructionStage), typeof(GSA1DInfluenceEffect) }, new Type[] { typeof(GSA1DElement) })]
  public class GSA1DElementPolyline : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElementPolyline();

    public void ParseGWACommand(List<GSA1DElement> elements)
    {
      if (elements.Count() < 1)
        return;

      var elementsListCopy = new List<GSA1DElement>(elements);

      var obj = new Structural1DElementPolyline
      {
        ApplicationId = Helper.GetApplicationId(typeof(GSA1DElementPolyline).GetGSAKeyword(), GSAId),

        Value = new List<double>(),
        ElementApplicationId = new List<string>(),

        ElementType = elementsListCopy.First().Value.ElementType,
        PropertyRef = elementsListCopy.First().Value.PropertyRef,
        ZAxis = new List<StructuralVectorThree>(),
        EndRelease = new List<StructuralVectorBoolSix>(),
        Offset = new List<StructuralVectorThree>(),
        ResultVertices = new List<double>()
      };

      if (Initialiser.Settings.Element1DResults.Count > 0 && Initialiser.Settings.EmbedResults)
        obj.Result = new Dictionary<string, object>();

      // Match up coordinates
      var coordinates = new List<Tuple<string, string>>();

      foreach (var e in elementsListCopy)
        coordinates.Add(new Tuple<string, string>(
          string.Join(",", (e.Value.Value as List<double>).Take(3).Select(x => Math.Round(x, 4).ToString())),
          string.Join(",", (e.Value.Value as List<double>).Skip(3).Take(3).Select(x => Math.Round(x, 4).ToString()))
        ));

      // Find start coordinate
      var flatCoordinates = coordinates.SelectMany(x => new List<string>() { x.Item1, x.Item2 }).ToList();
      var uniqueCoordinates = flatCoordinates.Where(x => flatCoordinates.Count(y => y == x) == 1).ToList();

      var current = uniqueCoordinates[0];

      //Because these properties could, depending on how they've been added to the StructuralProperties dictionary,
      //return another instance of the lists instead of a pointer to the lists themselves, temporary variables are used
      //to build up new lists which are assigned as replacements to the property values further down
      var elementAppIds = obj.ElementApplicationId;
      var zAxes = obj.ZAxis;
      var endReleases = obj.EndRelease;
      var offsets = obj.Offset;
      var resultVertices = obj.ResultVertices;

      while (coordinates.Count > 0)
      {
        var matchIndex = 0;
        var reverseCoordinates = false;

        matchIndex = coordinates.FindIndex(x => x.Item1 == current);
        reverseCoordinates = false;
        if (matchIndex == -1)
        {
          matchIndex = coordinates.FindIndex(x => x.Item2 == current);
          reverseCoordinates = true;
        }

        var element = elementsListCopy[matchIndex];

        elementAppIds.Add(element.Value.ApplicationId);
        zAxes.Add(element.Value.ZAxis);

        if (obj.Value.Count == 0)
        {
          if (!reverseCoordinates)
          {
            obj.Value.AddRange((element.Value.Value as List<double>).Take(3));
          }
          else
          {
            obj.Value.AddRange((element.Value.Value as List<double>).Skip(3).Take(3));
          }
        }

        if (!reverseCoordinates)
        {
          current = string.Join(",", (element.Value.Value as List<double>).Skip(3).Take(3).Select(x => Math.Round(x, 4).ToString()));
          obj.Value.AddRange((element.Value.Value as List<double>).Skip(3).Take(3));
          endReleases.AddRange(element.Value.EndRelease);
          offsets.AddRange(element.Value.Offset);

          if (Initialiser.Settings.Element1DResults.Count > 0 && Initialiser.Settings.EmbedResults)
          {
            resultVertices.AddRange(element.Value.ResultVertices);
          }
          else
          {
            resultVertices.AddRange((element.Value.Value as List<double>));
          }
        }
        else
        {
          current = string.Join(",", (element.Value.Value as List<double>).Take(3).Select(x => Math.Round(x, 4).ToString()));
          obj.Value.AddRange((element.Value.Value as List<double>).Take(3));
          endReleases.Add((element.Value.EndRelease as List<StructuralVectorBoolSix>).Last());
          endReleases.Add((element.Value.EndRelease as List<StructuralVectorBoolSix>).First());
          offsets.Add((element.Value.Offset as List<StructuralVectorThree>).Last());
          offsets.Add((element.Value.Offset as List<StructuralVectorThree>).First());

          if (Initialiser.Settings.Element1DResults.Count > 0 && Initialiser.Settings.EmbedResults)
          {
            for (int i = element.Value.ResultVertices.Count - 3; i >= 0; i -= 3)
            {
              resultVertices.AddRange((element.Value.ResultVertices as List<double>).Skip(i).Take(3));
            }
          }
          else
          {
            resultVertices.AddRange((element.Value.Value as List<double>).Skip(3).Take(3));
            resultVertices.AddRange((element.Value.Value as List<double>).Take(3));
          }
        }

        // Result merging
        if (obj.Result != null && ((Structural1DElement)element.Value).Result != null)
        {
          try
          {
            foreach (string loadCase in element.Value.Result.Keys)
            {
              if (!obj.Result.ContainsKey(loadCase))
                obj.Result[loadCase] = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                };

              var resultExport = element.Value.Result[loadCase] as Structural1DElementResult;

              if (resultExport != null)
              {
                foreach (var key in resultExport.Value.Keys)
                {
                  if (!(obj.Result[loadCase] as Structural1DElementResult).Value.ContainsKey(key))
                  {
                    (obj.Result[loadCase] as Structural1DElementResult).Value[key]
                      = new Dictionary<string, object>(resultExport.Value[key] as Dictionary<string, object>);
                  }
                  else
                  {
                    foreach (var resultKey in ((obj.Result[loadCase] as Structural1DElementResult).Value[key] as Dictionary<string, object>).Keys)
                    {
                      var res = (resultExport.Value[key] as Dictionary<string, object>)[resultKey] as List<double>;
                      if (reverseCoordinates)
                      {
                        res.Reverse();
                      }
                      (((obj.Result[loadCase] as Structural1DElementResult).Value[key] as Dictionary<string, object>)[resultKey] as List<double>)
                        .AddRange(res);
                    }
                  }
                }
              }
              else
              {
                // UNABLE TO MERGE RESULTS
                obj.Result = null;
                break;
              }
            }
          }
          catch
          {
            // UNABLE TO MERGE RESULTS
            obj.Result = null;
          }
        }

        coordinates.RemoveAt(matchIndex);
        elementsListCopy.RemoveAt(matchIndex);

        this.SubGWACommand.Add(element.GWACommand);
        this.SubGWACommand.AddRange(element.SubGWACommand);
      }

      obj.ElementApplicationId = elementAppIds;
      obj.ZAxis = zAxes;
      obj.EndRelease = endReleases;
      obj.Offset = offsets;
      obj.ResultVertices = resultVertices;

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var obj = this.Value as Structural1DElementPolyline;
      if (obj.Value == null || obj.Value.Count() == 0)
        return "";

      var elements = obj.Explode();
      var gwaCommands = new List<string>();

      if (elements.Count() == 1)
      {
        gwaCommands.Add((Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
         ? new GSA1DElement() { Value = elements.First() }.SetGWACommand()
         : new GSA1DMember() { Value = elements.First() }.SetGWACommand());
      }
      else
      {
        var group = Initialiser.Cache.ResolveIndex(typeof(GSA1DElementPolyline).GetGSAKeyword(), obj.ApplicationId);

        foreach (var element in elements)
        {
          gwaCommands.Add((Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
            ? new GSA1DElement() { Value = element }.SetGWACommand(group)
            : new GSA1DMember() { Value = element }.SetGWACommand(group));
        }
      }
      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpecklePolyline inputObject)
    {
      var convertedObject = new Structural1DElementPolyline();

      foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        var inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static string ToNative(this Structural1DElementPolyline poly)
    {
      return new GSA1DElementPolyline() { Value = poly }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA1DElementPolyline dummyObject)
    {
      var polylines = new List<GSA1DElementPolyline>();

      // Perform mesh merging
      var uniqueMembers = new List<string>(Initialiser.GSASenderObjects.Get<GSA1DElement>().Select(x => (x as GSA1DElement).Member).Where(m => Convert.ToInt32(m) > 0).Distinct());
      uniqueMembers.Sort();  //Just for readability and testing

      //This loop has been left as serial for now, considering the fact that the sender objects are retrieved and removed-from with each iteration
      foreach (var member in uniqueMembers)
      {
        try
        {
          var all1dElements = Initialiser.GSASenderObjects.Get<GSA1DElement>();
          var matching1dElementList = all1dElements.Where(x => (x as GSA1DElement).Member == member).OrderBy(m => m.GSAId).ToList();

          var poly = new GSA1DElementPolyline() { GSAId = Convert.ToInt32(member) };
          poly.ParseGWACommand(matching1dElementList);
          polylines.Add(poly);

          Initialiser.GSASenderObjects.RemoveAll(matching1dElementList);
        }
        catch { }
      }

      Initialiser.GSASenderObjects.AddRange(polylines);

      return new SpeckleNull(); // Return null because ToSpeckle method for GSA1DElement will handle this change
    }
  }
}

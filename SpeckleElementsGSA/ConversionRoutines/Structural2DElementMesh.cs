﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleElements;

namespace SpeckleElementsGSA
{
  // Keyword set as MEMB to not clash with grouping of members
  [GSAObject("MEMB.7", new string[] { }, "elements", true, false, new Type[] { typeof(GSA2DElement), typeof(GSA2DLoad), typeof(GSA2DElementResult), typeof(GSAAssembly), typeof(GSAStageDefinition) }, new Type[] { typeof(GSA2DProperty) })]
  public class GSA2DElementMesh : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DElementMesh();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSA2DElement> elements)
    {
      if (elements.Count() < 1)
        return;

      Structural2DElementMesh obj = new Structural2DElementMesh();
      obj.ApplicationId = GSA.GetSID(typeof(GSA2DElementMesh).GetGSAKeyword(), GSAId);

      obj.Vertices = new List<double>();
      obj.Faces = new List<int>();
      obj.ElementApplicationId = new List<string>();

      obj.ElementType = elements.First().Value.ElementType;
      obj.PropertyRef = elements.First().Value.PropertyRef;
      obj.Axis = new List<StructuralAxis>();
      obj.Offset = new List<double>();

      foreach (GSA2DElement e in elements)
      {
        int verticesOffset = obj.Vertices.Count() / 3;
        obj.Vertices.AddRange(e.Value.Vertices);
        obj.Faces.Add((e.Value.Faces as List<int>).First());
        obj.Faces.AddRange((e.Value.Faces as List<int>).Skip(1).Select(x => x + verticesOffset));

        obj.Axis.Add(e.Value.Axis);
        obj.Offset.Add(e.Value.Offset);

        obj.ElementApplicationId.Add(e.Value.ApplicationId);

        // Result merging
        if (Conversions.GSAElement2DResults.Count > 0)
        {
          if (obj.Result == null)
            obj.Result = new Dictionary<string, object>();

          try
          {
            foreach (string loadCase in e.Value.Result.Keys)
            {
              if (!obj.Result.ContainsKey(loadCase))
                obj.Result[loadCase] = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  IsGlobal = !Conversions.GSAResultInLocalAxis,
                };

              var resultExport = e.Value.Result[loadCase] as Structural2DElementResult;

              if (resultExport != null)
              {
                foreach (string key in resultExport.Value.Keys)
                {
                  if (!(obj.Result[loadCase] as Structural2DElementResult).Value.ContainsKey(key))
                    (obj.Result[loadCase] as Structural2DElementResult).Value[key] = new Dictionary<string, object>(resultExport.Value[key] as Dictionary<string, object>);
                  else
                    foreach (string resultKey in ((obj.Result[loadCase] as Structural2DElementResult).Value[key] as Dictionary<string, object>).Keys)
                      (((obj.Result[loadCase] as Structural2DElementResult).Value[key] as Dictionary<string, object>)[resultKey] as List<double>)
                        .AddRange((resultExport.Value[key] as Dictionary<string, object>)[resultKey] as List<double>);
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
            break;
          }
        }

        this.SubGWACommand.Add(e.GWACommand);
        this.SubGWACommand.AddRange(e.SubGWACommand);
      }

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural2DElementMesh obj = this.Value as Structural2DElementMesh;

      int group = GSA.Indexer.ResolveIndex(typeof(GSA2DElementMesh), obj);

      Structural2DElement[] elements = obj.Explode();

      foreach (Structural2DElement element in elements)
      {
        if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
          new GSA2DElement() { Value = element }.SetGWACommand(GSA, group);
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this SpeckleMesh inputObject)
    {
      Structural2DElementMesh convertedObject = new Structural2DElementMesh();

      foreach (PropertyInfo p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        PropertyInfo inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static bool ToNative(this Structural2DElementMesh mesh)
    {
      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        new GSA2DElementMesh() { Value = mesh }.SetGWACommand(GSA);
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
        new GSA2DMember() { Value = mesh }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA2DElementMesh dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA2DElementMesh)))
        GSASenderObjects[typeof(GSA2DElementMesh)] = new List<object>();

      List<GSA2DElementMesh> meshes = new List<GSA2DElementMesh>();

      // Perform mesh merging
      var uniqueMembers = new List<string>(GSASenderObjects[typeof(GSA2DElement)].Select(x => (x as GSA2DElement).Member).Distinct());
      foreach (string member in uniqueMembers)
      {
        var elementList = GSASenderObjects[typeof(GSA2DElement)].Where(x => (x as GSA2DElement).Member == member).Cast<GSA2DElement>().ToList();
        GSA2DElementMesh mesh = new GSA2DElementMesh() { GSAId = Convert.ToInt32(member) };
        mesh.ParseGWACommand(GSA, elementList);
        meshes.Add(mesh);

        GSASenderObjects[typeof(GSA2DElement)].RemoveAll(x => elementList.Contains(x));
      }

      GSASenderObjects[typeof(GSA2DElementMesh)].AddRange(meshes);

      return new SpeckleNull(); // Return null because ToSpeckle method for GSA2DElement will handle this change
    }
  }
}

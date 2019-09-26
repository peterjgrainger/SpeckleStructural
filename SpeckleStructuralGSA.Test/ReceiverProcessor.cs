﻿using System;
using System.Collections.Generic;
using System.Linq;
using Interop.Gsa_10_0;
using Moq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  //Copied from the Receiver class in SpeckleGSA - this will be refactored to simplify and avoid dependency
  public class ReceiverProcessor : ProcessorBase
  {
    private List<SpeckleObject> receivedObjects;

    public ReceiverProcessor(string directory, GSAInterfacer gsaInterfacer, GSATargetLayer layer = GSATargetLayer.Design) : base (directory)
    {
      GSAInterfacer = gsaInterfacer;
      Initialiser.Settings.TargetLayer = layer;
      ConstructTypeCastPriority(ioDirection.Receive, false);
    }

    public void JsonSpeckleStreamsToGwaRecords(IEnumerable<string> savedJsonFileNames, out List<GwaRecord> gwaRecords)
    {
      gwaRecords = new List<GwaRecord>();

      receivedObjects = JsonSpeckleStreamsToSpeckleObjects(savedJsonFileNames);

      GSAInterfacer.Indexer.SetBaseline();

      GSAInterfacer.PreReceiving();

      ScaleObjects();

      GSAInterfacer.Indexer.ResetToBaseline();

      ConvertSpeckleObjectsToGsaInterfacerCache();

      var gwaCommands = GSAInterfacer.GetSetCache();
      foreach (var gwaC in gwaCommands)
      {
        gwaRecords.Add(new GwaRecord(ExtractApplicationId(gwaC), gwaC));
      }
    }

    #region private_methods    

    private List<SpeckleObject> JsonSpeckleStreamsToSpeckleObjects(IEnumerable<string> savedJsonFileNames)
    {
      //Read JSON files into objects
      return ExtractObjects(savedJsonFileNames.ToArray(), TestDataDirectory);
    }

    private void ScaleObjects()
    {
      //Status.ChangeStatus("Scaling objects");
      var scaleFactor = (1.0).ConvertUnit("mm", "m");
      foreach (SpeckleObject o in receivedObjects)
      {
        try
        {
          o.Scale(scaleFactor);
        }
        catch { }
      }
    }

    private void ConvertSpeckleObjectsToGsaInterfacerCache()
    {
      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);

          var valueType = t.GetProperty("Value").GetValue(dummyObject).GetType();
          var targetObjects = receivedObjects.Where(o => o.GetType() == valueType);
          Converter.Deserialise(targetObjects);

          receivedObjects.RemoveAll(x => targetObjects.Any(o => x == o));

          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      // Write leftover
      Converter.Deserialise(receivedObjects);
    }

    private string ExtractApplicationId(string gwaCommand)
    {
      if (!gwaCommand.Contains(SID_TAG))
      {
        return null;
      }
      return gwaCommand.Split(new string[] { SID_TAG }, StringSplitOptions.None)[1].Substring(1).Split('}')[0];
    }


    public List<SpeckleObject> ExtractObjects(string fileName, string directory)
    {
      return ExtractObjects(new string[] { fileName }, directory);
    }

    public List<SpeckleObject> ExtractObjects(string[] fileNames, string directory)
    {
      var speckleObjects = new List<SpeckleObject>();
      foreach (var fileName in fileNames)
      {
        var json = Helper.ReadFile(fileName, directory);

        var response = ResponseObject.FromJson(json);
        speckleObjects.AddRange(response.Resources);
      }
      return speckleObjects;
    }

    #endregion
  }
}

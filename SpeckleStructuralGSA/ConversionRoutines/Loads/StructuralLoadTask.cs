﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ANAL.1", new string[] { "TASK.1" }, "loads", true, true, new Type[] { typeof(GSALoadCase) }, new Type[] { typeof(GSALoadCase) })]
  public class GSALoadTask : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadTask();

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      StructuralLoadTask obj = new StructuralLoadTask();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      //Find task type
      string taskRef = pieces[counter++];
      var taskRec = GSA.GetGWARecords("GET\tTASK.1\t" + taskRef).First();
      obj.TaskType = HelperClass.GetLoadTaskType(taskRec);
      this.SubGWACommand.Add(taskRec);

      // Parse description
      string description = pieces[counter++];
      obj.LoadCaseRefs = new List<string>();
      obj.LoadFactors = new List<double>();

      // TODO: this only parses the super simple linear add descriptions
      try
      {
        List<Tuple<string, double>> desc = HelperClass.ParseLoadDescription(description);

        foreach (Tuple<string, double> t in desc)
        {
          switch (t.Item1[0])
          {
            case 'L':
              obj.LoadCaseRefs.Add(GSA.GetSID(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(t.Item1.Substring(1))));
              obj.LoadFactors.Add(t.Item2);
              break;
          }
        }
      }
      catch
      {
      }

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralLoadTask loadTask = this.Value as StructuralLoadTask;

      string keyword = typeof(GSALoadTask).GetGSAKeyword();

      int taskIndex = GSA.Indexer.ResolveIndex("TASK.1", loadTask.ApplicationId);
      int index = GSA.Indexer.ResolveIndex(typeof(GSALoadTask).GetGSAKeyword(), loadTask.ApplicationId);

      List<string> ls = new List<string>();

      // Set TASK
      ls.Add("SET");
      ls.Add("TASK.1" + ":" + HelperClass.GenerateSID(loadTask));
      ls.Add(taskIndex.ToString());
      ls.Add(""); // Name
      ls.Add("0"); // Stage
      switch (loadTask.TaskType)
      {
        case StructuralLoadTaskType.LinearStatic:
          ls.Add("GSS");
          ls.Add("STATIC");
          // Defaults:
          ls.Add("1");
          ls.Add("0");
          ls.Add("128");
          ls.Add("SELF");
          ls.Add("none");
          ls.Add("none");
          ls.Add("DRCMEFNSQBHU*");
          ls.Add("MIN");
          ls.Add("AUTO");
          ls.Add("0");
          ls.Add("0");
          ls.Add("0");
          ls.Add("NONE");
          ls.Add("FATAL");
          ls.Add("NONE");
          ls.Add("NONE");
          ls.Add("RAFT_LO");
          ls.Add("RESID_NO");
          ls.Add("0");
          ls.Add("1");
          break;
        case StructuralLoadTaskType.NonlinearStatic:
          ls.Add("GSRELAX");
          ls.Add("BUCKLING_NL");
          // Defaults:
          ls.Add("SINGLE");
          ls.Add("0");
          ls.Add("BEAM_GEO_YES");
          ls.Add("SHELL_GEO_NO");
          ls.Add("0.1");
          ls.Add("0.0001");
          ls.Add("0.1");
          ls.Add("CYCLE");
          ls.Add("100000");
          ls.Add("REL");
          ls.Add("0.0010000000475");
          ls.Add("0.0010000000475");
          ls.Add("DISP_CTRL_YES");
          ls.Add("0");
          ls.Add("1");
          ls.Add("0.01");
          ls.Add("LOAD_CTRL_NO");
          ls.Add("1");
          ls.Add("");
          ls.Add("10");
          ls.Add("100");
          ls.Add("RESID_NOCONV");
          ls.Add("DAMP_VISCOUS");
          ls.Add("0");
          ls.Add("0");
          ls.Add("1");
          ls.Add("1");
          ls.Add("1");
          ls.Add("1");
          ls.Add("AUTO_MASS_YES");
          ls.Add("AUTO_DAMP_YES");
          ls.Add("FF_SAVE_ELEM_FORCE_YES");
          ls.Add("FF_SAVE_SPACER_FORCE_TO_ELEM");
          ls.Add("DRCEFNSQBHU*");
          break;
        case StructuralLoadTaskType.Modal:
          ls.Add("GSS");
          ls.Add("MODAL");
          // Defaults:
          ls.Add("1");
          ls.Add("1");
          ls.Add("128");
          ls.Add("SELF");
          ls.Add("none");
          ls.Add("none");
          ls.Add("DRCMEFNSQBHU*");
          ls.Add("MIN");
          ls.Add("AUTO");
          ls.Add("0");
          ls.Add("0");
          ls.Add("0");
          ls.Add("NONE");
          ls.Add("FATAL");
          ls.Add("NONE");
          ls.Add("NONE");
          ls.Add("RAFT_LO");
          ls.Add("RESID_NO");
          ls.Add("0");
          ls.Add("1");
          break;
        default:
          ls.Add("GSS");
          ls.Add("STATIC");
          // Defaults:
          ls.Add("1");
          ls.Add("0");
          ls.Add("128");
          ls.Add("SELF");
          ls.Add("none");
          ls.Add("none");
          ls.Add("DRCMEFNSQBHU*");
          ls.Add("MIN");
          ls.Add("AUTO");
          ls.Add("0");
          ls.Add("0");
          ls.Add("0");
          ls.Add("NONE");
          ls.Add("FATAL");
          ls.Add("NONE");
          ls.Add("NONE");
          ls.Add("RAFT_LO");
          ls.Add("RESID_NO");
          ls.Add("0");
          ls.Add("1");
          break;
      }
      GSA.RunGWACommand(string.Join("\t", ls));

      // Set ANAL
      ls.Clear();
      ls.Add("SET");
      ls.Add(keyword + ":" + HelperClass.GenerateSID(loadTask));
      ls.Add(index.ToString());
      ls.Add(loadTask.Name == null || loadTask.Name == "" ? " " : loadTask.Name);
      ls.Add(taskIndex.ToString());
      if (loadTask.TaskType == StructuralLoadTaskType.Modal)
        ls.Add("M1");
      else
      {
        List<string> subLs = new List<string>();
        for (int i = 0; i < loadTask.LoadCaseRefs.Count(); i++)
        {
          int? loadCaseRef = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), loadTask.LoadCaseRefs[i]);

          if (loadCaseRef.HasValue)
          {
            if (loadTask.LoadFactors.Count() > i)
              subLs.Add(loadTask.LoadFactors[i].ToString() + "L" + loadCaseRef.Value.ToString());
            else
              subLs.Add("L" + loadCaseRef.Value.ToString());
          }
        }
        ls.Add(string.Join(" + ", subLs));
      }
      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralLoadTask loadTask)
    {
      new GSALoadTask() { Value = loadTask }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSALoadTask dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSALoadTask)))
        GSASenderObjects[typeof(GSALoadTask)] = new List<object>();

      List<GSALoadTask> loadTasks = new List<GSALoadTask>();

      string keyword = typeof(GSALoadTask).GetGSAKeyword();
      string[] subKeywords = typeof(GSALoadTask).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSALoadTask)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSALoadTask)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSALoadTask task = new GSALoadTask() { GWACommand = p };
        task.ParseGWACommand(GSA);
        loadTasks.Add(task);
      }

      GSASenderObjects[typeof(GSALoadTask)].AddRange(loadTasks);

      if (loadTasks.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}

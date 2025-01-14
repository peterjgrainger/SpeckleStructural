﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("MAT_CONCRETE.17", new string[] { }, "properties", true, true, new Type[] { }, new Type[] { })]
  public class GSAMaterialConcrete : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralMaterialConcrete();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralMaterialConcrete();

      var pieces = this.GWACommand.ListSplit("\t");

      int commandVersion = 16;
      int.TryParse(pieces[0].Split(new[] { '.' }).Last(), out commandVersion);

      var counter = 1; // Skip identifier
      
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      counter++; // MAT.8
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Unlocked
      obj.YoungsModulus = Convert.ToDouble(pieces[counter++]);
      obj.PoissonsRatio = Convert.ToDouble(pieces[counter++]);
      obj.ShearModulus = Convert.ToDouble(pieces[counter++]);
      obj.Density = Convert.ToDouble(pieces[counter++]);
      obj.CoeffThermalExpansion = Convert.ToDouble(pieces[counter++]);

      obj.CompressiveStrength = Convert.ToDouble(pieces[41]);

      counter = (commandVersion == 16) ? 54 : 52;
      obj.MaxStrain = Convert.ToDouble(pieces[counter]);

      counter = (commandVersion == 16) ? 59 : 57;
      obj.AggragateSize = Convert.ToDouble(pieces[counter]);

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var mat = this.Value as StructuralMaterialConcrete;
      if (mat.ApplicationId == null)
      {
        return "";
      }

      var keyword = typeof(GSAMaterialConcrete).GetGSAKeyword();
      var index = Initialiser.Cache.ResolveIndex(typeof(GSAMaterialConcrete).GetGSAKeyword(), mat.ApplicationId);

      // TODO: This function barely works.
      var ls = new List<string>
      {
        "SET",
        "MAT_CONCRETE.17" + ":" + Helper.GenerateSID(mat),
        index.ToString(),
        "MAT.8",
        mat.Name == null || mat.Name == "" ? " " : mat.Name,
        "YES", // Unlocked
        (mat.YoungsModulus*1000).ToString(), // E
        mat.PoissonsRatio.ToString(), // nu
        mat.ShearModulus.ToString(), // G
        mat.Density.ToString(), // rho
        mat.CoeffThermalExpansion.ToString(), // alpha
        "MAT_ANAL.1",
        "Concrete",
        "-268435456", // TODO: What is this?
        "MAT_ELAS_ISO",
        "6", // TODO: What is this?
        mat.YoungsModulus.ToString(), // E
        mat.PoissonsRatio.ToString(), // nu
        mat.Density.ToString(), // rho
        mat.CoeffThermalExpansion.ToString(), // alpha
        mat.ShearModulus.ToString(), // G
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // Ultimate strain
        "MAT_CURVE_PARAM.2",
        "",
        "UNDEF",
        "1", // Material factor on strength
        "1", // Material factor on elastic modulus
        "MAT_CURVE_PARAM.2",
        "",
        "UNDEF",
        "1", // Material factor on strength
        "1", // Material factor on elastic modulus
        "0", // Cost
        "CYLINDER", // Strength type
        "N", // Cement class
        mat.CompressiveStrength.ToString(), // Concrete strength
        "0", //ls.Add("27912500"); // Uncracked strength
        "0", //ls.Add("17500000"); // Cracked strength
        "0", //ls.Add("2366431"); // Tensile strength
        "0", //ls.Add("2366431"); // Peak strength for curves
        "0", // TODO: What is this?
        "1", // Ratio of initial elastic modulus to secant modulus
        "2", // Parabolic coefficient
        "0.00218389285990043", // SLS strain at peak stress
        "0.0035", // SLS max strain
        "0.00041125", // ULS strain at plateau stress
        mat.MaxStrain.ToString(), // ULS max compressive strain
        "0.0035", // TODO: What is this?
        "0.002", // Plateau strain
        "0.0035", // Max axial strain
        "NO", // Lightweight?
        mat.AggragateSize.ToString(), // Aggragate size
        "0", // TODO: What is this?
        "1", // TODO: What is this?
        "0.8825", // Constant stress depth
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0" // TODO: What is this?
      };

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralMaterialConcrete mat)
    {
      return new GSAMaterialConcrete() { Value = mat }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAMaterialConcrete dummyObject)
    {
      var newLines = ToSpeckleBase<GSAMaterialConcrete>();

      var materialsLock = new object();
      var materials = new List<GSAMaterialConcrete>();

      Parallel.ForEach(newLines.Values, p =>
      {
        try
        {
          var mat = new GSAMaterialConcrete() { GWACommand = p };
          mat.ParseGWACommand();
          lock (materialsLock)
          {
            materials.Add(mat);
          }
        }
        catch { }
      });

      Initialiser.GSASenderObjects.AddRange(materials);

      return (materials.Count() > 0 ) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}

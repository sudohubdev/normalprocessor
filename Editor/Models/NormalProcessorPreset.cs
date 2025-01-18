using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace dev.sudohub.normalprocessor 
{
    [Serializable]
    public struct Preset {
        public AnimationCurve bwCurve;
        public float smoothness;
        public float intensity;
        public bool doTiling;
        public bool useScharr;
        public string name;
        public Preset(string presetname){
            bwCurve = AnimationCurve.Linear(0, 0, 1, 1);
            smoothness = 2;
            intensity = 2;
            doTiling = false;
            useScharr = false;
            name = presetname;
        }
    }

    [FilePath("dev.sudohub.normalprocessor/PresetData.dat", FilePathAttribute.Location.PreferencesFolder)]
    public class PresetData : ScriptableSingleton<PresetData>
    {
        public List<Preset> presets = new();

        private void Save()
        {
            Save(true); // Save to disk
        }

        public void Select(Preset preset)
        {
            //move chosen preset to top
            presets.Remove(preset);
            presets.Insert(0, preset);
            Save();
        }
        public void Add(Preset preset){
            presets.Insert(0, preset);
            //limit to 10
            if(presets.Count > 5){
                presets.RemoveAt(5);
            }
            Save();
        }
        public void Clear(){
            presets.Clear();
            Save();
        }
        public IEnumerator<Preset> GetEnumerator(){
            return presets.GetEnumerator();
        }
    }
}
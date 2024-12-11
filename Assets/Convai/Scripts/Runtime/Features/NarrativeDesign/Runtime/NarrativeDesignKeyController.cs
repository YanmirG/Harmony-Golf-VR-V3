using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    public class NarrativeDesignKeyController : MonoBehaviour
    {
        public List<NarrativeDesignKey> narrativeDesignKeys;
        
        [Serializable]
        public class NarrativeDesignKey
        {
            public string name;
            public string value;
        }
        
        public void SetTemplateKey(Dictionary<string, string> keyValuePairs)
        {
            narrativeDesignKeys.Clear();
            narrativeDesignKeys.AddRange(from item in keyValuePairs
                select new NarrativeDesignKey { name = item.Key, value = item.Value });
        }
        public void AddTemplateKey(string name, string value)
        {
            narrativeDesignKeys.Add(new NarrativeDesignKey { name = name, value = value });
        }
        public void RemoveTemplateKey(string name)
        {
            NarrativeDesignKey reference = narrativeDesignKeys.Find(x => x.name == name);
            if(reference == null) return;
            narrativeDesignKeys.Remove(reference);
        }
        public void UpdateTemplateKey(string name, string value)
        {
            NarrativeDesignKey reference = narrativeDesignKeys.Find(x => x.name == name);
            if (reference == null) return;
            reference.value = value;
        }
    }
}
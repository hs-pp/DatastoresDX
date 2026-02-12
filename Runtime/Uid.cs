using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DatastoresDX.Runtime
{
    [Serializable]
    public struct Uid
    {
        private static string ID_PREFIX = "ID-";
        public static Uid Invalid => new(0);

        [SerializeField, HideInInspector]
        private int m_value;
        public int Value => m_value;
        
        public Uid(int value)
        {
            m_value = value;
        }

        public bool IsInvalid()
        {
            return Equals(Invalid);
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Uid otherId)
            {
                return m_value.Equals(otherId.m_value);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return m_value;
        }

        public override string ToString()
        {
            return $"{ID_PREFIX}{ToBase62(m_value)}";
        }

        public static Uid FromString(string base64String)
        {
            base64String = base64String.Replace(ID_PREFIX, "");
            return new Uid(FromBase62(base64String));
        }
        
        /// <summary>
        /// Mostly just for serialization. This shouldn't be used normally.
        /// </summary>
        public void SetDirectly(int value)
        {
            m_value = value;
        }

#region Base62 Conversion
        private static readonly char[] Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        private static readonly Dictionary<char, uint> Base62CharMap = new();
        private static Dictionary<int, string> m_idToBase62Lookup = new();

        public static string ToBase62(Uid uid)
        {
            return ToBase62(uid.Value);
        }
        
        private static string ToBase62(int id)
        {
            if (!m_idToBase62Lookup.ContainsKey(id))
            {
                StringBuilder base62String = new StringBuilder();

                uint unsigned = (uint)(id - int.MinValue);
                while (unsigned > 0) // convert
                {
                    base62String.Insert(0, Base62Chars[unsigned % 62]);
                    unsigned /= 62;
                }
                    
                while (base62String.Length < 6) // pad string with extra 0's to always ensure 13 char.
                {
                    base62String.Insert(0, '0');
                }
                
                m_idToBase62Lookup.Add(id, base62String.ToString());
            }
            
            return m_idToBase62Lookup[id];
        }

        private static int FromBase62(string base62String)
        {
            if (Base62CharMap.Count == 0)
            {
                FillCharMap();
            }
            
            uint id = 0;
            foreach (char c in base62String)
            {
                id = id * 62 + Base62CharMap[c];
            }
            int signed = (int)(id + int.MinValue);
            return signed;
        }

        private static void FillCharMap()
        {
            Base62CharMap.Clear();
            for (uint i = 0; i < Base62Chars.Length; i++)
            {
                Base62CharMap[Base62Chars[i]] = i;
            }
        }
#endregion

#if UNITY_EDITOR
        public static Uid FromSerializedProperty(SerializedProperty elementSP)
        {
            return new Uid(elementSP.FindPropertyRelative(Value_VarName).intValue);
        }

        public static void ToSerializedProperty(SerializedProperty elementSP, Uid id)
        {
            elementSP.FindPropertyRelative(Value_VarName).intValue = id.m_value;
        }
        
        public static readonly string Value_VarName = "m_value";
#endif
    }
}
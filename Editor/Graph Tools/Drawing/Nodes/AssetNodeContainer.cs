using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VaporGraphTools;

namespace VaporGraphToolsEditor
{
    public class AssetNodeContainer
    {
        public string Name { get; }
        public string AssetPath { get; }

        public Type AssetType { get; }

        public AssetNodeContainer(string name, string assetPath, Type assetType)
        {
            Name = name;
            AssetPath = assetPath;
            AssetType = assetType;
        }

        public virtual Type GetNodeType() { return AssetType; }
    }

    public class AssetNodeContainer<T> : AssetNodeContainer where T : NodeSo
    {
        public AssetNodeContainer(string name, string assetPath, Type assetType) : base(name, assetPath, assetType)
        {
        }

        public override Type GetNodeType()
        {
            return typeof(T);
        }
    }
}

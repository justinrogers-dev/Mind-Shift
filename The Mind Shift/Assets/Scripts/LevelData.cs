using UnityEngine;
using System.Collections.Generic;

namespace LevelManagement
{
    [System.Serializable]
    public class PathCondition
    {
        public string pathConditionName;
        public List<Condition> conditions;
        public List<SinglePath> paths;
    }

    [System.Serializable]
    public class Condition
    {
        public Transform conditionObject;
        public Vector3 eulerAngle;
    }

    [System.Serializable]
    public class SinglePath
    {
        public Walkable block;
        public int index;
    }
}
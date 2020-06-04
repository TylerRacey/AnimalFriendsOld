using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class Common
    {
        public enum VoxelFaces
        {
            FRONT,
            RIGHT,
            TOP,
            LEFT,
            BOTTOM,
            BACK,
            SIZE
        }

        public const float VOXEL_SIZE = 0.10f;
    }

    public class Axis
    {
        public const string HORIZONTAL = "Horizontal";
        public const string VERTICAL = "Vertical";
    }

    public class MouseAxis
    {
        public const string MOUSE_Y = "Mouse Y";
        public const string MOUSE_X = "Mouse X";
    }

    public class Math
    {
        public const double COS_10 = 0.98480775301;
        public const double COS_30 = 0.86602540378;
    }

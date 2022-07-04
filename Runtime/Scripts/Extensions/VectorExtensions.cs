﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using UnityEngine;

namespace SPACS.Utilities
{
    public static class VectorExtensions
    {
        ///////////////////////////////////////////////////////////////////////////
        public static Vector3 Average(this IEnumerable<Vector3> vectors)
        {
            Vector3 average = Vector3.zero;

            foreach (Vector3 vector in vectors)
                average += vector;
            average /= vectors.Count();

            return average;
        }

        ///////////////////////////////////////////////////////////////////////////
        public static Vector3? DeserializeVector3(this string sourceString)
        {
            if (sourceString == null || sourceString == string.Empty)
            {
                return null;
            }

            // Trim extranious parenthesis and split delimted values into an array
            string[] splitString = sourceString.Substring(1, sourceString.Length - 2).Split(", "[0]);

            // Build new Vector3 from array elements
            return new Vector3(
                float.Parse(splitString[0], CultureInfo.InvariantCulture), 
                float.Parse(splitString[1], CultureInfo.InvariantCulture), 
                float.Parse(splitString[2], CultureInfo.InvariantCulture));
        }

    }
}
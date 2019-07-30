﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NeoCortexApi.Entities;
using NeoCortexApi.Utility;

namespace NeoCortexApi.Network
{
    public class HtmClassifier<TIN, TOUT>
    {
        private Dictionary<int[], TIN> activeMap = new Dictionary<int[], TIN>();

        private Dictionary<int[], TIN> predictMap = new Dictionary<int[], TIN>();

        private Dictionary<TIN, int[]> activeArray = new Dictionary<TIN, int[]>();

        public void Learn(TIN input, Cell[] output, Cell[] predictedOutput)
        {
            if (!activeMap.ContainsKey(FlatArray1(output)))
            {
                this.activeMap.Add(FlatArray1(output), input);
            }

            if (!activeArray.ContainsKey(input))
            {
                this.activeArray.Add(input, FlatArray1(output));
            }

            if (!predictMap.ContainsKey(FlatArray1(predictedOutput)))
            {
                this.predictMap.Add(FlatArray1(predictedOutput), input);
            }
        }

        /// <summary>
        /// Get corresponding input value for current cycle.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public TIN GetInputValue(Cell[] output)
        {
            if (output.Length != 0 && activeMap.ContainsKey(FlatArray1(output)))
            {
                return activeMap[FlatArray1(output)];
            }
            return default(TIN);
        }


        /// <summary>
        /// Gets predicted value for next cycle
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public String GetPredictedInputValue(Cell[] output)
        {
            int result = 0;
            string charOutput = null;
            int[] arr = new int[output.Length];
            for (int i = 0; i < output.Length; i++)
            {
                arr[i] = output[i].Index;
            }
            if (output.Length != 0)
            {
                foreach (TIN inputVal in activeArray.Keys)
                {
                    int count = predictNextValue(arr, activeArray[inputVal]);
                    if (count > result)
                    {
                        result = count;
                        charOutput = inputVal as String;
                    }
                }
                return charOutput;
                //return activeMap[ComputeHash(FlatArray(output))];
            }
            return null;
        }

        
        private string ComputeHash(byte[] rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(rawData);

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        

        
        private static byte[] FlatArray(Cell[] output)
        {
            byte[] arr = new byte[output.Length];
            for (int i = 0; i < output.Length; i++)
            {
                arr[i] = (byte)output[i].Index;
            }
            return arr;
        }

        private static int[] FlatArray1(Cell[] output)
        {
            int[] arr = new int[output.Length];
            for (int i = 0; i < output.Length; i++)
            {
                arr[i] = output[i].Index;
            }
            return arr;
        }

        private int predictNextValue(int[] activeArr, int[] predictedArr)
        {
            var same = predictedArr.Intersect(activeArr);

            return same.Count();
        }
    }
}

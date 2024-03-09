﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NeoCortexApi;
using NeoCortexApi.Entities;
using System.Net.Http.Headers;
using Naa = NeoCortexApi.NeuralAssociationAlgorithm;
using System.Diagnostics;


namespace UnitTestsProject
{
    /// <summary>
    /// UnitTests for the Cell.
    /// </summary>
    [TestClass]
    public class SdrReconstructionTests
    {
        [TestMethod]
        [TestCategory("Prod")]
        public void Reconstruct_AddsKeyIfNotExists()
        {
            var cfg = UnitTestHelpers.GetHtmConfig(100, 1024);
            Connections mem = new Connections(cfg);
            SpatialPoolerMT sp = new SpatialPoolerMT();
            sp.Init(mem);
            SPSdrReconstructor reconstructor = new SPSdrReconstructor(mem);

            int[] activeMiniColumns = new int[] { 1, 2, 3, 4, 5, 7, 20, 54, 700 };

            Dictionary<int, double> permanences = reconstructor.Reconstruct(activeMiniColumns);

            Assert.IsNotNull(permanences);

            Assert.IsTrue(permanences.ContainsKey(1));


        }
        public void Reconstruct_ReturnsValidDictionary()
        {
            var cfg = UnitTestHelpers.GetHtmConfig(100, 1024);
            Connections mem = new Connections(cfg);
            SpatialPoolerMT sp = new SpatialPoolerMT();
            sp.Init(mem);
            SPSdrReconstructor reconstructor = new SPSdrReconstructor(mem);


            int[] activeMiniColumns = new int[] { 1, 2, 3 };
            Dictionary<int, double> permanences = reconstructor.Reconstruct(activeMiniColumns);
            Assert.IsNotNull(permanences);


        }



        public void Reconstruct_AtLeastOneNegativePermanence_ReturnsFalse()
        {

            var cfg = UnitTestHelpers.GetHtmConfig(100, 1024);
            Connections mem = new Connections(cfg);
            SpatialPoolerMT sp = new SpatialPoolerMT();
            sp.Init(mem);
            SPSdrReconstructor reconstructor = new SPSdrReconstructor(mem);
            int[] activeMiniColumns = new int[] { 1, 2, 3, 4, 5 };

            Dictionary<int, double> permanences = reconstructor.Reconstruct(activeMiniColumns);

            Assert.IsNotNull(permanences);

            Assert.IsFalse(permanences.Values.Any(value => value < 0), "At least one permanence value should be negative");


        }
        public void Reconstruct_InvalidDictionary_ReturnsFalse()
        {

            var cfg = UnitTestHelpers.GetHtmConfig(100, 1024);
            Connections mem = new Connections(cfg);
            SpatialPoolerMT sp = new SpatialPoolerMT();
            sp.Init(mem);
            SPSdrReconstructor reconstructor = new SPSdrReconstructor(mem);
            int[] activeMiniColumns = new int[] { 1, 2, 3 };

            Dictionary<int, double> permanences = reconstructor.Reconstruct(activeMiniColumns);
            Assert.IsFalse(IsDictionaryInvalid(permanences), "Result should be false for an invalid dictionary");



        }
        private bool IsDictionaryInvalid(Dictionary<int, double> dictionary)
        {


            if (dictionary == null)
            {
                return true;
            }
            
            if (dictionary.Values.Any(value => double.IsNaN(value)) || dictionary.Keys.Any(key => key < 0))
            {
                return true;
            }

            return false;



        }



    }
}

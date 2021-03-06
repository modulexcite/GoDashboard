﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using GoDashboard.Web.Models;
using NUnit.Framework;

namespace GoDashboard.Web.Tests.Model
{
    [TestFixture]
    public class TestPipelineGroupModel
    {
        private const string Branch = "TESTPipelineGroup";

        [Test]
        public void Should_Have_Failed_Status_If_Contains_Failed_Pipeline()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline>
                              {
                                  new Pipeline{LastBuildStatus = "Passed"},
                                  new Pipeline{LastBuildStatus = "Failure"},
                                  new Pipeline{LastBuildStatus = "Passed"}
                              }
            };

            Assert.AreEqual(pipelineGroup.Status, "failed");
        }

        [Test]
        public void Should_Have_Passed_Status_If_Doesnt_Contain_Failed_Pipeline()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline>
                              {
                                  new Pipeline{LastBuildStatus = "Passed"},
                                  new Pipeline{LastBuildStatus = "Passed"},
                                  new Pipeline{LastBuildStatus = "Passed"}
                              }
            };

            Assert.AreEqual(pipelineGroup.Status, "passed");
        }

        [Test]
        public void Should_Be_Fix_Overdue_When_Pipeline_Has_Failed_For_30_Minutes()
        {
            var pipelineGroup = new PipelineGroup
                                    {
                                        Name = Branch,
                                        Pipelines = new List<Pipeline> { new Pipeline { LastBuildStatus = "Failure", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-31) } }
                                    };
            Assert.AreEqual(pipelineGroup.FixOverdue, "overdue");
        }

        [Test]
        public void Should_Not_Be_Fix_Overdue_When_Pipeline_Has_Failed_For_Less_Than_30_Minutes()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline> { new Pipeline { LastBuildStatus = "Failure", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-29) } }
            };
            Assert.AreEqual(pipelineGroup.FixOverdue, "good");
        }

        [Test]
        public void Should_Not_Be_Fix_Overdue_When_Trunk_Has_Failed_For_Less_Then_10_Minutes()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = "Trunk",
                Pipelines = new List<Pipeline> { new Pipeline { LastBuildStatus = "Failure", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-9) } }
            };
            Assert.AreEqual(pipelineGroup.FixOverdue, "good");
        }

        [Test]
        public void Should_Not_Be_Fix_Overdue_When_Pipeline_Has_Passed()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline> { new Pipeline { LastBuildStatus = "Passed", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-31) } }
            };
            Assert.AreEqual(pipelineGroup.FixOverdue, "good");
        }

        [Test]
        public void Should_Set_LastBuildTime_Of_Failing_Pipeline_When_Contains_Passing_Pipeline_Of_Later_Build()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline>
                    {
                        new Pipeline { LastBuildStatus = "Failure", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-31) },
                        new Pipeline { LastBuildStatus = "Passed", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-9) }
                    }
            };

            var timeDifference = DateTime.Now.AddMinutes(-31) - pipelineGroup.LastBuildTimeDateTime;

            Assert.That(timeDifference.Minutes, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Should_Set_LastBuildTime_Of_Last_Built_Pipeline_When_All_Passed()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline>
                                {
                                    new Pipeline { LastBuildStatus = "Passed", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-31) },
                                    new Pipeline { LastBuildStatus = "Passed", LastBuildTimeDateTime = DateTime.Now.AddMinutes(-9) }
                                }
            };

            var timeDifference = DateTime.Now.AddMinutes(-9) - pipelineGroup.LastBuildTimeDateTime;

            Assert.That(timeDifference.Minutes, Is.LessThanOrEqualTo(0));
        }

        [Test]
        public void Should_Set_LastBuildLabelPipeline_Of_Latest_Pipeline_Revision()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline>
                                {
                                    new Pipeline { Name = "One", LastBuildStatus = "Passed", LastBuildLabel = "2" },
                                    new Pipeline { Name = "Two", LastBuildStatus = "Passed", LastBuildLabel = "6" },
                                    new Pipeline { Name = "Three", LastBuildStatus = "Passed", LastBuildLabel = "1" },
                                    new Pipeline { Name = "Four", LastBuildStatus = "Passed", LastBuildLabel = "5" }
                                }
            };

            Assert.AreEqual(pipelineGroup.LastBuildLabelPipeline.Name, "Two");
        }

        [Test]
        public void Should_Set_LastBuildLabelPipeline_Of_Earliest_Failed_Pipeline_Revision()
        {
            var pipelineGroup = new PipelineGroup
            {
                Name = Branch,
                Pipelines = new List<Pipeline>
                                {
                                    new Pipeline { Name = "Two", LastBuildStatus = "Passed", LastBuildLabel = "6" },
                                    new Pipeline { Name = "One", LastBuildStatus = "Failure", LastBuildLabel = "2" },
                                    new Pipeline { Name = "Four", LastBuildStatus = "Failure", LastBuildLabel = "5" },
                                    new Pipeline { Name = "Three", LastBuildStatus = "Passed", LastBuildLabel = "1" }
                                }
            };

            Assert.AreEqual(pipelineGroup.LastBuildLabelPipeline.Name, "One");
        }

        [Test]
        public void FriendlyName_Should_Be_Friendly()
        {
            var pipelineGroup = new PipelineGroup
                                    {
                                        Name = "UNFRIENDLY.NAME"
                                    };

            Assert.AreEqual(pipelineGroup.FriendlyName, "UNFRIENDLYNAME");
        }

        [Test]
        public void AddPipeline_Should_Add_Pipeline()
        {
            const string PipelineXml = "<Project name=\"Trunk\" "
                                       + "activity=\"Sleeping\""
                                       + " lastBuildStatus=\"Success\""
                                       + " lastBuildLabel=\"68636\""
                                       + " lastBuildTime=\"2011-12-02T11:01:04\""
                                       + " webUrl=\"http://go/pipelines/CI/618/build/1\"/>";
            var xDoc = new XmlDocument();
            xDoc.LoadXml(PipelineXml);
            var xmlNode = xDoc.GetElementsByTagName("Project")[0];
            var pipelineGroup = new PipelineGroup();
            pipelineGroup.AddPipeline(xmlNode);

            var pipeline = pipelineGroup.Pipelines.First();
            Assert.That(pipeline.Name, Is.EqualTo("Trunk"));
            Assert.That(pipeline.Activity, Is.EqualTo("Sleeping"));
            Assert.That(pipeline.LastBuildStatus, Is.EqualTo("Success"));
            Assert.That(pipeline.LastBuildLabel, Is.EqualTo("68636"));
            Assert.That(pipeline.LastBuildTime, Is.EqualTo("12/02/2011 11:01:04"));
            Assert.That(pipeline.WebUrl, Is.EqualTo("http://go/pipelines/CI/618/build/1"));
        }
    }
}

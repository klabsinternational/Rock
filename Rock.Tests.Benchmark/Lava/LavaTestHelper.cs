// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rock.Lava;
using Rock.Tests.Integration.Lava.Benchmarks.Rock.Tests.Benchmark.Lava;
using Rock.Tests.Shared;

namespace Rock.Tests.Benchmark.Lava
{
    public class LavaTestHelper
    {
        //ILavaTemplate _lavaTemplate;

        //public TestHelper( ILavaTemplate fixture )
        //{
        //    _lavaTemplate = fixture;

        //}

        //public ILavaTemplate LavaTemplate { get { return _lavaTemplate; } }

        /// <summary>
        /// Process the specified input template and return the result.
        /// </summary>
        /// <param name="inputTemplate"></param>
        /// <returns></returns>
        //public string GetTemplateOutput( string inputTemplate )
        //{
        //    string outputString;

        //    inputTemplate = inputTemplate ?? string.Empty;

        //    bool isValidTemplate = _lavaTemplate.TryRender( inputTemplate.Trim(), out outputString );

        //    Assert.That.True( isValidTemplate, "Fluid Template is invalid." );

        //    return outputString;
        //}

        /// <summary>
        /// Process the specified input template and verify against the expected output.
        /// </summary>
        /// <param name="expectedOutput"></param>
        /// <param name="inputTemplate"></param>
        //public void AssertTemplateOutput( string expectedOutput, string inputTemplate )
        //{
        //    var outputString = GetTemplateOutput( inputTemplate );

        //    Assert.That.AreEqual( expectedOutput, outputString );
        //}

        /// <summary>
        /// Process the specified input template and verify against the expected output regular expression.
        /// </summary>
        /// <param name="expectedOutputRegex"></param>
        /// <param name="inputTemplate"></param>
        //public void AssertTemplateOutputRegex( string expectedOutputRegex, string inputTemplate )
        //{
        //    var outputString = GetTemplateOutput( inputTemplate );

        //    //Assert.That..ma.Matches( expectedOutputRegex, outputString );
        //}

        /// <summary>
        /// Process the specified input template and verify the output against an expected DateTime result.
        /// </summary>
        /// <param name="expectedDateTime"></param>
        /// <param name="inputTemplate"></param>
        /// <param name="maximumDelta"></param>
        //public void AssertTemplateOutputDate( DateTime? expectedDateTime, string inputTemplate, TimeSpan? maximumDelta = null )
        //{
        //    var outputString = GetTemplateOutput( inputTemplate );

        //    DateTime outputDate;

        //    var isValidDate = DateTime.TryParse( outputString, out outputDate );

        //    Assert.True( isValidDate, $"Template Output does not represent a valid DateTime. [Output=\"{ outputString }\"]" );

        //    if ( maximumDelta != null )
        //    {
        //        DateTimeAssert.AreEqual( expectedDateTime, outputDate, maximumDelta.Value );
        //    }
        //    else
        //    {
        //        DateTimeAssert.AreEqual( expectedDateTime, outputDate );
        //    }
        //}

        /// <summary>
        /// Resolve the specified template to a date and verify that it is equivalent to the expected date.
        /// </summary>
        /// <param name="expectedDateString"></param>
        /// <param name="inputTemplate"></param>
        /// <param name="maximumDelta"></param>
        //public void AssertTemplateOutputDate( string expectedDateString, string inputTemplate, TimeSpan? maximumDelta = null )
        //{
        //    bool isValid;
        //    DateTime expectedDate;

        //    isValid = DateTime.TryParse( expectedDateString, out expectedDate );

        //    Assert.That.IsTrue( isValid, "Expected Date String input is not a valid date." );

        //    AssertTemplateOutputDate( expectedDate, inputTemplate, maximumDelta );
        //}

        /// <summary>
        /// Return an initialized Person object for test subject Ted Decker.
        /// </summary>
        /// <returns></returns>
        public TestPerson GetTestPersonTedDecker()
        {
            var campus = new TestCampus { Name = "North Campus", Id = 1 };
            var person = new TestPerson { FirstName = "Edward", NickName = "Ted", LastName = "Decker", Campus = campus, Id = 1 };

            return person;
        }

        /// <summary>
        /// Return an initialized Person object for test subject Ted Decker.
        /// </summary>
        /// <returns></returns>
        public TestPerson GetTestPersonAlishaMarble()
        {
            var campus = new TestCampus { Name = "South Campus", Id = 2 };
            var person = new TestPerson { FirstName = "Alisha", NickName = "Alisha", LastName = "Marble", Campus = campus, Id = 2 };

            return person;
        }

        /// <summary>
        /// Return a collection of initialized Person objects for the Decker family.
        /// </summary>
        /// <returns></returns>
        public List<TestPerson> GetTestPersonCollectionForDecker()
        {
            var personList = new List<TestPerson>();

            personList.Add( GetTestPersonTedDecker() );
            personList.Add( new TestPerson { FirstName = "Cindy", LastName = "Decker", Id = 2 } );
            personList.Add( new TestPerson { FirstName = "Noah", LastName = "Decker", Id = 3 } );
            personList.Add( new TestPerson { FirstName = "Alex", LastName = "Decker", Id = 4 } );

            return personList;
        }
    }        
}

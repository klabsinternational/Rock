using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using DotLiquid;
using Rock.Web.Cache;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rock.Tests.Benchmark.Lava
{
    /// <summary>
    /// A set of benchmarks to test various algorithms for caching and retrieving Lava Templates.
    /// </summary>
    [TestClass]
    public class LavaTemplateCacheBenchmarkTests
    {
        [ClassInitialize]
        public static void Initialize( TestContext context )
        {
            // Verify that caching is available, or the benchmarks will be meaningless.
            if ( RockCache.IsCacheSerialized )
            {
                throw new Exception( "Caching is unavailable for Lava templates in the current environment." );
            }
        }

        /// <summary>
        /// Benchmark a variety of algorithms for caching larger Lava Templates.
        /// </summary>
        [TestMethod]
        public void LavaTemplateCache_VariousCacheKeyAlgorithms_GetBenchmarks()
        {
            TestTemplateRetrieveAndRender( "Lava Template Caching Strategy Benchmark: Retrieve Only", renderTemplate: false );
            TestTemplateRetrieveAndRender( "Lava Template Caching Strategy Benchmark: Retrieve and Render", renderTemplate: true );
        }

        /// <summary>
        /// Benchmark a variety of algorithms for caching larger Lava Templates.
        /// </summary>
        private void TestTemplateRetrieveAndRender( string testName, bool renderTemplate )
        {
            var templateList = new List<string>()
            {
                LavaTemplateTiny, LavaTemplateSmall, LavaTemplateMedium, LavaTemplateLarge
            };

            // Verify the template sizes
            Assert.IsTrue( templateList.Any( x => x.Length <= 32 ), "One of the test templates must be sized so that: x <= 32 bytes" );
            Assert.IsTrue( templateList.Any( x => x.Length > 32 && x.Length <= 100 ), "One of the test templates must be sized so that: 32 < x <= 100 bytes" );
            Assert.IsTrue( templateList.Any( x => x.Length > 100 && x.Length < 1000 ), "One of the test templates must be sized so that: 100 < x < 1000 bytes." );
            Assert.IsTrue( templateList.Any( x => x.Length > 1000 ), "One of the test templates must be sized so that: x > 1000 bytes." );

            var mergeObjects = new Dictionary<string, object>();
            var helper = new LavaTestHelper();

            var personList = helper.GetTestPersonCollectionForDecker();

            mergeObjects.Add( "Person1", personList[0] );
            mergeObjects.Add( "Person2", personList[1] );
            mergeObjects.Add( "Person3", personList[2] );
            mergeObjects.Add( "TextValue1", "This is text value 1.");

            Debug.Print( "\n***\n*** [{0}] ({1})\n***\n", testName, DateTime.Now.ToISO8601DateString() );

            GetBenchmarksForTemplates( templateList, mergeObjects, testName, renderTemplate );
        }

        private void GetBenchmarksForTemplates( List<string> templateList, Dictionary<string, object> mergeObjects, string testSetName, bool renderTemplate )
        {
            Debug.Print( "*** Cache Strategy: Rock v11 (cache if less than 100 chars)", testSetName );

            GetBenchmarkForCacheRetrievalMethod( templateList, mergeObjects, renderTemplate, GetTemplateByContentOrCreate );

            Debug.Print( "*** Cache Strategy: MD5 Hash (content hash only)", testSetName );

            GetBenchmarkForCacheRetrievalMethod( templateList, mergeObjects, renderTemplate, GetCachedTemplateByMD5HashKey );

            Debug.Print( "*** Cache Strategy: MD5 Hash/Raw (hash or raw content if less than hash size)", testSetName );

            GetBenchmarkForCacheRetrievalMethod( templateList, mergeObjects, renderTemplate, GetCachedTemplateByMD5HashKeyOrContent );

            Debug.Print( "*** Cache Strategy: xxHash Hash/Raw (hash or raw content if less than hash size)", testSetName );

            GetBenchmarkForCacheRetrievalMethod( templateList, mergeObjects, renderTemplate, GetCachedTemplateByXXHashKey );
        }

        private void GetBenchmarkForCacheRetrievalMethod( List<string> templates, Dictionary<string, object> mergeObjects, bool renderTemplate, Func<string, Template> cacheRetrievalFunction )
        {
            var stopwatchAll = new Stopwatch();
            var stopwatchPass = new Stopwatch();

            var mergeHash = Hash.FromDictionary( mergeObjects );

            int _totalPassCount = 10;
            int _iterationsPerSet = 1000;

            Debug.Print( "--- Benchmark started [Sets={0}, IterationsPerSet={1}, TemplatesResolvedPerIteration={2}]...", _totalPassCount, _iterationsPerSet, templates.Count );

            for ( var passCount = 1; passCount <= _totalPassCount; passCount++ )
            {
                // Force garbage collection now to ensure that it does not happen unexpectedly during the benchmark run.
                stopwatchAll.Stop();

                GC.Collect();
                LavaTemplateCache.Clear();

                _cacheHitCount = 0;
                _cacheMissCount = 0;

                stopwatchAll.Start();

                stopwatchPass.Restart();

                for ( var i = 1; i <= _iterationsPerSet; i++ )
                {
                    // Process each of the template in turn.
                    foreach ( var templateText in templates )
                    {
                        var template = cacheRetrievalFunction( templateText );

                        if ( renderTemplate )
                        {
                            var output = template.Render( mergeHash );

                            if ( template.Errors.Any() )
                            {
                                throw new Exception( "Lava Template rendering failed." );
                            }
                        }
                    }
                }

                stopwatchPass.Stop();

                Debug.Print( "--- Pass {0:00} of {1} completed. ({2} ms, {3}/{4} cache hits/misses)", passCount, _totalPassCount, stopwatchPass.ElapsedMilliseconds, _cacheHitCount, _cacheMissCount );
            }

            stopwatchAll.Stop();

            Debug.Print( "--- Benchmark completed. ({0} ms)", stopwatchAll.ElapsedMilliseconds );
        }

        private static ulong _cacheHitCount;
        private static ulong _cacheMissCount;

        private static Template GetOrAddCachedLavaTemplate( string templateKey, string content )
        {
            var isCacheHit = LavaTemplateCache.ContainsKey( templateKey );

            if ( isCacheHit )
            {
                if ( _cacheHitCount < ulong.MaxValue )
                {
                    _cacheHitCount++;
                }
            }
            else
            {
                if ( _cacheMissCount < ulong.MaxValue )
                {
                    _cacheMissCount++;
                }

            }

            var template = LavaTemplateCache.Get( templateKey, content ).Template;

            // Clear any previous errors
            template.Errors.Clear();

            return template;
        }

        #region Cache Retrieval Functions

        /*
         * These are the alternate template retrieval functions for testing the hashing agorithm for template caching.
         * The signature of these methods should be identical to the current LavaExtensions.GetTemplate method,
         * and they must implement the same functionality to ensure the benchmark comparison is accurate.
         */

        /// <summary>
        /// Retrieve a template from the cache if it is less than 100 characters in size,
        /// otherwise return a new template instance.
        /// This is the strategy for retrieving templates used in Rock v11.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private Template GetTemplateByContentOrCreate( string content )
        {
            // Do not cache any content over 100 characters in length.
            if ( content.Length > 100 )
            {
                _cacheMissCount++;

                return Template.Parse( content );
            }

            // Get template from cache
            return GetOrAddCachedLavaTemplate( content, content );
        }

        /// <summary>
        /// Cache Key is MD5 hash of the content.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private Template GetCachedTemplateByMD5HashKey( string content )
        {
            // Get template from cache
            var templateKey = content.Md5Hash();

            return GetOrAddCachedLavaTemplate( templateKey, content );
        }

        /// <summary>
        /// Cache Key is MD5, or the content itself if it is shorter than an MD5 hash.
        /// This *should* represent an optimization of the simple MD5 hash strategy.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private Template GetCachedTemplateByMD5HashKeyOrContent( string content )
        {
            const int hashLength = 32;

            if ( string.IsNullOrEmpty( content ) )
            {
                return GetOrAddCachedLavaTemplate( string.Empty, content );
            }
            else if ( content.Length <= hashLength )
            {
                // If the content is less than the size of an MD5 hash (128-bit),
                // simply use the content as the key to save processing time.
                return GetOrAddCachedLavaTemplate( content, content );
            }
            else
            {
                var templateKey = content.Md5Hash();

                return GetOrAddCachedLavaTemplate( templateKey, content );
            }
        }

        private Template GetCachedTemplateByXXHashKey( string content )
        {
            const int hashLength = 10;

            if ( string.IsNullOrEmpty( content ) )
            {
                return GetOrAddCachedLavaTemplate( string.Empty, content );
            }
            else if ( content.Length <= hashLength )
            {
                // If the content is less than the size of an MD5 hash (128-bit),
                // simply use the content as the key to save processing time.
                return GetOrAddCachedLavaTemplate( content, content );
            }
            else
            {
                var templateKey = xxHashSharp.xxHash.CalculateHash( Encoding.UTF8.GetBytes( content ) ).ToString();

                return GetOrAddCachedLavaTemplate( templateKey, content );
            }
        }

        #endregion

        #region Test Data

        // Tiny Lava Template (Size < 32 bytes).
        private const string LavaTemplateTiny = @"
{{ TextValue1 }}
";

        // Small Lava Template (Size < 100 bytes).
        private const string LavaTemplateSmall = @"
{{ Person1 | Property:'FirstName' }} {{ Person1 | Property:'LastName' }}
";

        // Medium-sized template (Size ~500 bytes).
        private const string LavaTemplateMedium = @"
[{{ Person1 | Property:'Id' }}] {{ Person1 | Property:'FirstName' }} {{ Person1 | Property:'LastName' }}
[{{ Person2 | Property:'Id' }}] {{ Person2 | Property:'FirstName' }} {{ Person2 | Property:'LastName' }}
[{{ Person3 | Property:'Id' }}] {{ Person3 | Property:'FirstName' }} {{ Person3 | Property:'LastName' }}
";

        // Large Lava Template (Size ~28K).
        // This is the "Sidebar" Communication Template from the default Rock installation (v11.1).
        private const string LavaTemplateLarge = @"
<!DOCTYPE html>
<html>


<head>
  <meta http-equiv='Content-Type' content='text/html; charset=utf-8'>
  <meta name = 'viewport' content='width=device-width'>
  <title>My Basic Email Template Subject</title>
  <!-- <style> -->
</head>


<body style = '-moz-box-sizing: border-box; -ms-text-size-adjust: 100%; -webkit-box-sizing: border-box; -webkit-text-size-adjust: 100%; Margin: 0; background: {{ bodyBackgroundColor }} !important; box-sizing: border-box; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0; min-width: 100%; padding: 0; text-align: left; width: 100% !important;' >
  < div id='lava-fields' style='display:none'>
    <!-- Lava Fields: Code-Generated from Template Editor -->
    {% assign headerBackgroundColor = '#5e5e5e' %}
    {% assign linkColor = '#2199e8' %}
    {% assign bodyBackgroundColor = '#f3f3f3' %}
    {% assign textColor = '#0a0a0a' %}
    {% assign sidebarBackgroundColor = '#e0e0e0' %}
    {% assign sidebarBorderColor = '#bdbdbd' %}
    {% assign sidebarTextColor = '#0a0a0a' %}
  </div>

  <style>
      a {
          color: {{ linkColor }};
      }
      
      .component-text td
{
    color: { { textColor } };
    font-family: Helvetica, Arial, sans-serif;
    font-size: 16px;
    font-weight: normal;
    line-height: 1.3;
}
      
      .sidebar.component-text td
{
    color: { { sidebarTextColor } };
    font-family: Helvetica, Arial, sans-serif;
    font-size: 16px;
    font-weight: normal;
    line-height: 1.3;
}
      
      .sidebar.component-text h1,
      .sidebar.component-text h2,
      .sidebar.component-text h3,
      .sidebar.component-text h4,
      .sidebar.component-text h5,
      .sidebar.component-text h6
{
    Margin: 0;
    Margin-bottom: 10px;
    color: inherit;
    font-family: Helvetica, Arial, sans-serif;
    font-size: 20px;
    font-weight: normal;
    line-height: 1.3;
    margin: 0;
    margin-bottom: 10px;
    padding: 0;
    text-align: left;
    word-wrap: normal;
}
  </style>

  <style class='ignore'>
    @media only screen {
      html {
        min-height: 100%;
        background: {{ bodyBackgroundColor }};
      }
    }
    
    @media only screen and( max-width: 596px)
{
      .small - float - center {
        margin:
        0 auto!important;
        float: none!important;
        text - align: center!important;
    }
      .small - text - center {
        text - align: center!important;
    }
      .small - text - left {
        text - align: left!important;
    }
      .small - text - right {
        text - align: right!important;
    }
}

@media only screen and( max-width: 596px)
{
      .hide -for-large {
        display:
        block!important;
        width:
        auto!important;
        overflow:
        visible!important;
        max - height: none!important;
        font - size: inherit!important;
        line - height: inherit!important;
    }
}

@media only screen and( max-width: 596px)
{
    table.body table.container.hide -for-large,
    table.body table.container.row.hide -for-large {
            display:
            table!important;
            width:
            100 % !important;
        }
}

@media only screen and( max-width: 596px)
{
    table.body table.container.callout - inner.hide -for-large {
        display:
        table - cell!important;
        width:
        100 % !important;
    }
}

@media only screen and( max-width: 596px)
{
    table.body table.container.show -for-large {
        display:
        none!important;
        width:
        0;
        mso - hide: all;
        overflow:
        hidden;
    }
}

@media only screen and( max-width: 596px)
{
    table.body img {
        width:
        auto;
        height:
        auto;
    }
    table.body center {
        min - width: 0!important;
    }
    table.body.container {
        width:
        95 % !important;
    }
    table.body.columns,
      table.body.column {
        height:
        auto!important;
        -moz - box - sizing: border - box;
        -webkit - box - sizing: border - box;
        box - sizing: border - box;
        padding - left: 16px!important;
        padding - right: 16px!important;
    }
    table.body.columns.column,
      table.body.columns.columns,
      table.body.column.column,
      table.body.column.columns {
        padding - left: 0!important;
        padding - right: 0!important;
    }
    table.body.collapse.columns,
      table.body.collapse.column {
        padding - left: 0!important;
        padding - right: 0!important;
    }
    td.small - 1,
      th.small - 1 {
        display:
        inline - block!important;
        width:
        8.33333 % !important;
    }
    td.small - 2,
      th.small - 2 {
        display:
        inline - block!important;
        width:
        16.66667 % !important;
    }
    td.small - 3,
      th.small - 3 {
        display:
        inline - block!important;
        width:
        25 % !important;
    }
    td.small - 4,
      th.small - 4 {
        display:
        inline - block!important;
        width:
        33.33333 % !important;
    }
    td.small - 5,
      th.small - 5 {
        display:
        inline - block!important;
        width:
        41.66667 % !important;
    }
    td.small - 6,
      th.small - 6 {
        display:
        inline - block!important;
        width:
        50 % !important;
    }
    td.small - 7,
      th.small - 7 {
        display:
        inline - block!important;
        width:
        58.33333 % !important;
    }
    td.small - 8,
      th.small - 8 {
        display:
        inline - block!important;
        width:
        66.66667 % !important;
    }
    td.small - 9,
      th.small - 9 {
        display:
        inline - block!important;
        width:
        75 % !important;
    }
    td.small - 10,
      th.small - 10 {
        display:
        inline - block!important;
        width:
        83.33333 % !important;
    }
    td.small - 11,
      th.small - 11 {
        display:
        inline - block!important;
        width:
        91.66667 % !important;
    }
    td.small - 12,
      th.small - 12 {
        display:
        inline - block!important;
        width:
        100 % !important;
    }
      .columns td.small - 12,
      .column td.small - 12,
      .columns th.small - 12,
      .column th.small - 12 {
        display:
        block!important;
        width:
        100 % !important;
    }
    table.body td.small - offset - 1,
      table.body th.small - offset - 1 {
        margin - left: 8.33333 % !important;
        Margin - left: 8.33333 % !important;
    }
    table.body td.small - offset - 2,
      table.body th.small - offset - 2 {
        margin - left: 16.66667 % !important;
        Margin - left: 16.66667 % !important;
    }
    table.body td.small - offset - 3,
      table.body th.small - offset - 3 {
        margin - left: 25 % !important;
        Margin - left: 25 % !important;
    }
    table.body td.small - offset - 4,
      table.body th.small - offset - 4 {
        margin - left: 33.33333 % !important;
        Margin - left: 33.33333 % !important;
    }
    table.body td.small - offset - 5,
      table.body th.small - offset - 5 {
        margin - left: 41.66667 % !important;
        Margin - left: 41.66667 % !important;
    }
    table.body td.small - offset - 6,
      table.body th.small - offset - 6 {
        margin - left: 50 % !important;
        Margin - left: 50 % !important;
    }
    table.body td.small - offset - 7,
      table.body th.small - offset - 7 {
        margin - left: 58.33333 % !important;
        Margin - left: 58.33333 % !important;
    }
    table.body td.small - offset - 8,
      table.body th.small - offset - 8 {
        margin - left: 66.66667 % !important;
        Margin - left: 66.66667 % !important;
    }
    table.body td.small - offset - 9,
      table.body th.small - offset - 9 {
        margin - left: 75 % !important;
        Margin - left: 75 % !important;
    }
    table.body td.small - offset - 10,
      table.body th.small - offset - 10 {
        margin - left: 83.33333 % !important;
        Margin - left: 83.33333 % !important;
    }
    table.body td.small - offset - 11,
      table.body th.small - offset - 11 {
        margin - left: 91.66667 % !important;
        Margin - left: 91.66667 % !important;
    }
    table.body table.columns td.expander,
      table.body table.columns th.expander {
        display:
        none!important;
    }
    table.body.right - text - pad,
      table.body.text - pad - right {
        padding - left: 10px!important;
    }
    table.body.left - text - pad,
      table.body.text - pad - left {
        padding - right: 10px!important;
    }
    table.menu {
        width:
        100 % !important;
    }
    table.menu td,
    table.menu th {
        width:
        auto!important;
        display:
        inline - block!important;
    }
    table.menu.vertical td,
    table.menu.vertical th,
    table.menu.small - vertical td,
      table.menu.small - vertical th {
        display:
        block!important;
    }
    table.menu[align = 'center'] {
        width:
        auto!important;
    }
    table.button.small - expand,
      table.button.small - expanded {
        width:
        100 % !important;
    }
    table.button.small - expand table,
      table.button.small - expanded table {
        width:
        100 %;
    }
    table.button.small - expand table a,
      table.button.small - expanded table a {
        text - align: center!important;
        width:
        100 % !important;
        padding - left: 0!important;
        padding - right: 0!important;
    }
    table.button.small - expand center,
      table.button.small - expanded center {
        min - width: 0;
    }
}
  </style>
  <span class='preheader' style='color: {{ bodyBackgroundColor }}; display: none !important; font-size: 1px; line-height: 1px; max-height: 0px; max-width: 0px; mso-hide: all !important; opacity: 0; overflow: hidden; visibility: hidden;'></span>
  <table class='body' style='Margin: 0; background: {{ bodyBackgroundColor }} !important; border-collapse: collapse; border-spacing: 0; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; height: 100%; line-height: 1.3; margin: 0; padding: 0; text-align: left; vertical-align: top; width: 100%;'>
    <tbody>
      <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
        < td class='center' align='center' valign='top' style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 1.3; margin: 0; padding: 0; text-align: left; vertical-align: top; word-wrap: break-word; background-color: {{ bodyBackgroundColor }};'>
          <center data-parsed='' style='min-width: 580px; width: 100%;'>


            <table align = 'center' class='wrapper header float-center' style='Margin: 0 auto; background: {{ headerBackgroundColor }}; border-collapse: collapse; border-spacing: 0; float: none; margin: 0 auto; padding: 0; text-align: center; vertical-align: top; width: 100%;'>
              <tbody>
                <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                  < td class='wrapper-inner' style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 1.3; margin: 0; padding: 15px; text-align: left; vertical-align: top; word-wrap: break-word;'>
                    <table align = 'center' class='container' style='Margin: 0 auto; background: transparent; border-collapse: collapse; border-spacing: 0; margin: 0 auto; padding: 0; text-align: inherit; vertical-align: top; width: 580px;'>
                      <tbody>
                        <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                          < td style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 1.3; margin: 0; padding: 0; text-align: left; vertical-align: top; word-wrap: break-word;'>
                            <!-- LOGO -->
                            <img id = 'template-logo' src='/Content/EmailTemplates/placeholder-logo.png' width='200' height='50' data-instructions='Provide a PNG with a transparent background.' style='display: block;'>  
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </td>
                </tr>
              </tbody>
            </table>


            <table align = 'center' class='container float-center' style='Margin: 0 auto; background: #fefefe; border-collapse: collapse; border-spacing: 0; float: none; margin: 0 auto; padding: 0; text-align: center; vertical-align: top; width: 580px;'>
              <tbody>
                <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                  < td style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 1.3; margin: 0; padding: 0; text-align: left; vertical-align: top; word-wrap: break-word;'>


                    <table class='spacer' style='border-collapse: collapse; border-spacing: 0; padding: 0; text-align: left; vertical-align: top; width: 100%;'>
                      <tbody>
                        <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                          < td height='16px' style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 16px; margin: 0; mso-line-height-rule: exactly; padding: 0; text-align: left; vertical-align: top; word-wrap: break-word;'>&nbsp;</td>
                        </tr>
                      </tbody>
                    </table>


                    <table class='row' style='border-collapse: collapse; border-spacing: 0; display: table; padding: 0; position: relative; text-align: left; vertical-align: top; width: 100%;'>
                      <tbody>
                        <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                          < th class='small-12 large-12 columns first last' style='Margin: 0 auto; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0 auto; padding: 0; padding-bottom: 16px; padding-left: 16px; padding-right: 16px; text-align: left; width: 564px;'>
							
							<table style = 'border-collapse: collapse; border-spacing: 0; display: table; padding: 0; position: relative; text-align: left; vertical-align: top; width: 100%;' >

                                < tbody >

                                    < tr >

                                        < td valign='top' style='width: 67%'>
							                <div class='structure-dropzone'>
                								<div class='dropzone'>
                								    <table class='component component-text selected' data-state='component' style='border-collapse: collapse; border-spacing: 0px; display: table; padding: 0px; position: relative; text-align: left; vertical-align: top; width: 100%; background-color: transparent;'>
                							            <tbody>
                							                <tr>
                							                    <td class='js-component-text-wrapper' style='border-color: rgb(0, 0, 0);'>
                							                        <h1>Title</h1><p> Can't wait to see what you have to say!</p>
                						                        </td>
                					                        </tr>
                				                        </tbody>
                			                        </table>
                
                								</div>
                							</div>
							            </td>
							            <td style = 'width: 3%' > </ td >

                                        < td valign='top' style='width: 40%'>
							                
							                <table class='sidebar' style='border-collapse: collapse; border-spacing: 0; display: table; padding: 0; position: relative; text-align: left; vertical-align: top; width: 100%;'>
							                    <tbody>
							                        <tr>
							                            <td style = 'background-color: {{ sidebarBackgroundColor }}; border: 1px solid {{ sidebarBorderColor }}; color: {{ sidebarTextColor }}; padding: 6px;' >

                                                            < div class='structure-dropzone'>
                                								<div class='dropzone'>
                                								    <table class='component component-text selected' data-state='component' style='color: {{ sidebarTextColor }}; border-collapse: collapse; border-spacing: 0px; display: table; padding: 0px; position: relative; text-align: left; vertical-align: top; width: 100%; background-color: transparent;'>
                                							            <tbody>
                                							                <tr>
                                							                    <td class='js-component-text-wrapper' style='border-color: rgb(0, 0, 0);'>
                                							                        <h5 style = 'Margin: 0; Margin-bottom: 10px; color: inherit; font-family: Helvetica, Arial, sans-serif; font-size: 20px; font-weight: normal; line-height: 1.3; margin: 0; margin-bottom: 10px; padding: 0; text-align: left; word-wrap: normal;' > Title </ h5 >< p > Place your sidebar content here.</p>
                                         						                        </td>
                                					                        </tr>
                                				                        </tbody>
                                			                        </table>
                                
                                								</div>
                                							</div>
							                            </td>
							                        </tr>
							                    </tbody>
							                </table>
							                
							                <!-- spacer -->
							                <table style = 'border-collapse: collapse; border-spacing: 0; display: table; padding: 0; position: relative; text-align: left; vertical-align: top; width: 100%;' >
         
                                                         < tbody >
         
                                                             < tr >
         
                                                                 < td style= 'height: 24px;' ></ td >
         
                                                             </ tr >
         
                                                         </ tbody >
         
                                                     </ table >
         

                                                     < table style= 'border-collapse: collapse; border-spacing: 0; display: table; padding: 0; position: relative; text-align: left; vertical-align: top; width: 100%;' >
         
                                                         < tbody >
         
                                                             < tr >
         
                                                                 < td style= 'background-color: {{ sidebarBackgroundColor }}; border: 1px solid {{ sidebarBorderColor }}; color: {{ sidebarTextColor }}; padding: 6px;' >
         
                                                                     < h5 style= 'Margin: 0; Margin-bottom: 10px; color: inherit; font-family: Helvetica, Arial, sans-serif; font-size: 20px; font-weight: normal; line-height: 1.3; margin: 0; margin-bottom: 10px; padding: 0; text-align: left; word-wrap: normal;' > Contact Info:</h5>
                                                            <p style = 'margin: 0 0 0 10px;margin-bottom: 10px;color: {{ sidebarTextColor }};font-family: Helvetica, Arial, sans-serif;font-size: 16px;font-weight: normal;line-height: 1.3;padding: 0;text-align: left;' > Website: <a href = '{{ 'Global' | Attribute:'OrganizationWebsite' }}' style= 'Margin: 0; color: {{ linkColor }}; font-family: Helvetica, Arial, sans-serif; font-weight: normal; line-height: 1.3; margin: 0; padding: 0; text-align: left; text-decoration: none;' >{ { 'Global' | Attribute:'OrganizationWebsite' } }</a></p>
                                                            <p style = 'margin: 0 0 0 10px;margin-bottom: 10px;color: {{ sidebarTextColor }};font-family: Helvetica, Arial, sans-serif;font-size: 16px;font-weight: normal;line-height: 1.3;padding: 0;text-align: left;' > Email: <a href = 'mailto:{{ 'Global' | Attribute:'OrganizationEmail' }}' style='Margin: 0; color: {{ linkColor }}; font-family: Helvetica, Arial, sans-serif; font-weight: normal; line-height: 1.3; margin: 0; padding: 0; text-align: left; text-decoration: none;'>{{ 'Global' | Attribute:'OrganizationEmail' }}</a></p>
							                            </td>
							                        </tr>
							                    </tbody>
							                </table>
							                
							            </td>
							        </tr>
							    </tbody>
							</table>
							
							
							
						  </th>
                        </tr>
                      </tbody>
                    </table>
                    <table class='wrapper secondary' align='center' style='background: {{ bodyBackgroundColor }}; border-collapse: collapse; border-spacing: 0; padding: 0; text-align: left; vertical-align: top; width: 100%;'>
                      <tbody>
                        <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                          < td class='wrapper-inner' style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 1.3; margin: 0; padding: 0; text-align: left; vertical-align: top; word-wrap: break-word;'>


                            <table class='spacer' style='border-collapse: collapse; border-spacing: 0; padding: 0; text-align: left; vertical-align: top; width: 100%;'>
                              <tbody>
                                <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                                  < td height='16px' style='-moz-hyphens: auto; -webkit-hyphens: auto; Margin: 0; border-collapse: collapse !important; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; hyphens: auto; line-height: 16px; margin: 0; mso-line-height-rule: exactly; padding: 0; text-align: left; vertical-align: top; word-wrap: break-word;'>&nbsp;</td>
                                </tr>
                              </tbody>
                            </table>


                            <table class='row' style='border-collapse: collapse; border-spacing: 0; display: table; padding: 0; position: relative; text-align: left; vertical-align: top; width: 100%;'>
                              <tbody>
                                <tr style = 'padding: 0; text-align: left; vertical-align: top;' >
                                  < th class='small-12 large-6 columns first' style='Margin: 0 auto; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0 auto; padding: 0; padding-bottom: 16px; padding-left: 16px; padding-right: 8px; text-align: left; width: 274px;'>
                                    <table style = 'border-collapse: collapse; border-spacing: 0; padding: 0; text-align: left; vertical-align: top; width: 100%;' >
                                      < tbody >
                                        < tr style='padding: 0; text-align: left; vertical-align: top;'>
                                          <th style = 'Margin: 0; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0; padding: 0; text-align: left;' >
                                            &nbsp;
                                          </th>
                                        </tr>
                                      </tbody>
                                    </table>
                                  </th>
                                  <th class='small-12 large-6 columns last' style='Margin: 0 auto; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0 auto; padding: 0; padding-bottom: 16px; padding-left: 8px; padding-right: 16px; text-align: left; width: 274px;'>
                                    <table style = 'border-collapse: collapse; border-spacing: 0; padding: 0; text-align: left; vertical-align: top; width: 100%;' >
                                      < tbody >
                                        < tr style='padding: 0; text-align: left; vertical-align: top;'>
                                          <th style = 'Margin: 0; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0; padding: 0; text-align: left;' >
                                            < h5 style='Margin: 0; Margin-bottom: 10px; color: inherit; font-family: Helvetica, Arial, sans-serif; font-size: 20px; font-weight: normal; line-height: 1.3; margin: 0; margin-bottom: 10px; padding: 0; text-align: left; word-wrap: normal;'>Contact Info:</h5>
                                            <p style = 'Margin: 0; Margin-bottom: 10px; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0; margin-bottom: 10px; padding: 0; text-align: left;' > Website: <a href = '{{ 'Global' | Attribute:'OrganizationWebsite' }}' style='Margin: 0; color: {{ linkColor }}; font-family: Helvetica, Arial, sans-serif; font-weight: normal; line-height: 1.3; margin: 0; padding: 0; text-align: left; text-decoration: none;'>{{ 'Global' | Attribute:'OrganizationWebsite' }}</a></p>
                                            <p style = 'Margin: 0; Margin-bottom: 10px; color: {{ textColor }}; font-family: Helvetica, Arial, sans-serif; font-size: 16px; font-weight: normal; line-height: 1.3; margin: 0; margin-bottom: 10px; padding: 0; text-align: left;' > Email: <a href = 'mailto:{{ 'Global' | Attribute:'OrganizationEmail' }}' style='Margin: 0; color: {{ linkColor }}; font-family: Helvetica, Arial, sans-serif; font-weight: normal; line-height: 1.3; margin: 0; padding: 0; text-align: left; text-decoration: none;'>{{ 'Global' | Attribute:'OrganizationEmail' }}</a></p>
                                          </th>
                                        </tr>
                                      </tbody>
                                    </table>
                                  </th>
                                </tr>
                              </tbody>
                            </table>
							<div style = 'width: 100%; text-align: center; font-size: 11px; font-color: #6f6f6f; margin-bottom: 24px;' >[[UnsubscribeOption]]</div>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </td>
                </tr>
              </tbody>
            </table>


          </center>
        </td>
      </tr>
    </tbody>
  </table>
  <!-- prevent Gmail on iOS font size manipulation -->
  <div style = 'display:none; white-space:nowrap; font:15px courier; line-height:0;' > &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; </div>
</body>
</html>
";

        #endregion
    }
}

var gulp = require('gulp');
var $ = require('gulp-load-plugins')({ lazy: true });

var del = require('del');
var fs = require('fs');
var request = require('request');
var path = require('path');
var runSequence = require('run-sequence');
var merge = require('merge-stream');
var xmlpoke = require('xmlpoke');
var replace = require('gulp-replace');
var config = require('./gulp.config')();

// the msbuild configuration
var buildConfiguration = "Debug";

// the build number and version number of this product
var buildNumber = 9999;
if(process.env.APPVEYOR_BUILD_NUMBER)
    buildNumber = process.env.APPVEYOR_BUILD_NUMBER;
var versionNumber = "0.0.0." + buildNumber;

gulp.task('default', ['local'], function() {

});

gulp.task('local', function(cb) {
  runSequence(
    'compile',
    'test',
    function (error) {
      if (error) {
        console.log(error.message);
      }
      cb(error);
    });
});

gulp.task('ci', function(cb) {
  runSequence(
    'clean',
    'config-release',
    'local',
    'dist',
    function (error) {
      if (error) {
        console.log(error.message);
      }
      cb(error);
    });
});

gulp.task('clean', function() {
  return del([
    path.resolve(__dirname, 'build'),
    path.resolve(__dirname, 'dist')
  ]);
});

gulp.task('config-release', function() {
  buildConfiguration = "Release";
});

gulp.task('dist', ['dist-web', 'dist-sub-worker', 'dist-static'], function() {

});

gulp.task('dist-web', function(cb) {
  runSequence(
    'dist-web-copy',
    'dist-web-fix-clearscript',
    'dist-web-configuration',
    'dist-web-make-static',
    function (error) {
      if (error) {
        console.log(error.message);
      }
      cb(error);
    });
});

gulp.task('dist-web-fix-clearscript', function(){
  gulp.src('./dist/web/bin/{ClearScriptV8-32,ClearScriptV8-64,v8-ia32,v8-x64}.dll')
    .pipe($.clean())
    .pipe(gulp.dest('./dist/web/bin/ClearScript/'));
});

gulp.task('dist-web-configuration', function(){
  gulp.src('./dist/web/Web.config')
    .pipe(replace(/debug="(true|false)"/g, 'debug="' + (buildConfiguration == "Debug" ? 'true' : 'false') + '"'))
    .pipe(gulp.dest('./dist/web/'));
});

gulp.task('dist-web-make-static', function(cb) {
  runSequence(
    'dist-web-delete-static',
    'dist-web-configure-static',
    function (error) {
      if (error) {
        console.log(error.message);
      }
      cb(error);
    });
});

gulp.task('dist-web-copy', function() {
  return gulp.src(path.resolve(__dirname, 'build', '_PublishedWebsites', 'Skimur.Web.Public') + "/**/*")
    .pipe(gulp.dest(path.resolve(__dirname, 'dist', 'web')));
});

gulp.task('dist-web-delete-static', function() {
  return del([
  	// delete everything that will be part of the 'static' site
  	'./dist/web/Content/', 
  	'./dist/web/Scripts']);
});

gulp.task('dist-web-configure-static', function(cb) {
  xmlpoke('./dist/web/Web.config', function(xml) {
    xml.withBasePath('configuration')
      .set("appSettings/add[@key='UseStaticAssets']/@value", config.useStaticAssets)
      .set("appSettings/add[@key='StaticAssetsHost']/@value", config.staticAssetsHost)
  });
  cb();
});

gulp.task('dist-sub-worker', function() {
  return gulp.src(path.resolve(__dirname, 'build', 'Subs.Worker.Cons') + "/**/*")
    .pipe(gulp.dest(path.resolve(__dirname, 'dist', 'worker')));
});

gulp.task('dist-static', function(cb) {
  runSequence(
    'dist-static-copy',
    'dist-static-copy-config',
    'dist-static-compile-less',
    'dist-static-compile-js',
    'dist-static-clean',
    function (error) {
      if (error) {
        console.log(error.message);
      }
      cb(error);
    });
});

gulp.task('dist-static-copy', function() {
  return gulp.src(
  	[
  	  './build/_PublishedWebsites/Skimur.Web.Public/Content/**/*', 
  	  './build/_PublishedWebsites/Skimur.Web.Public/Scripts/**/*'
  	], 
  	{
  	  base: './build/_PublishedWebsites/Skimur.Web.Public/'
  	})
    .pipe(gulp.dest('./dist/static'));
});

gulp.task('dist-static-compile-less', function() {
  return gulp.src('./dist/static/Content/site.less')
    .pipe($.less({
    	relativeUrls: true
    }))
    .pipe($.minifyCss({keepSpecialComments:0}))
    .pipe(gulp.dest('./dist/static/Content'));
});

gulp.task('dist-static-compile-js', function() {
  var jsFiles = config.jsFiles.slice(0);
  for(var i = 0; i < jsFiles.length; i++)
    jsFiles[i] = './dist/static/Scripts/' + jsFiles[i];
  return gulp.src(jsFiles)
    .pipe($.uglify())
    .pipe($.concat('script.js'))
    .pipe(gulp.dest('./dist/static/Scripts'));
});

gulp.task('dist-static-clean', function() {
  return del([
  	// delete everything!
  	'./dist/static/Content/**/*', 
  	'./dist/static/Scripts/**/*',
  	// except!
  	'!./dist/static/Content/**/*.css', // leave all css files alone
  	'!./dist/static/Content/img/', '!./dist/static/Content/img/**', // leave the img directory
  	'!./dist/static/Content/fonts/', '!./dist/static/Content/fonts/**',
  	'!./dist/static/Scripts/script.js', // and leave the fonts directory
  	'!./dist/static/web.config']) 
});

gulp.task('dist-static-copy-config', function() {
  return gulp.src('./src/static.web.config')
    .pipe($.rename('web.config'))
    .pipe(gulp.dest('./dist/static'));
});

gulp.task('compile', ['nuget-restore', 'version-assemblies'], function() {
  return gulp
    .src('**/*.sln')
    .pipe($.msbuild({
      toolsVersion: 14.0,
      targets: ['Clean', 'Build'],
      errorOnFail: true,
      stdout: true,
      configuration: buildConfiguration,
      properties : {
        CI:true,
        OutputPath: path.resolve(__dirname, 'build')
      }
    }));
});

gulp.task('test', function() {
  // todo
});

gulp.task('version-assemblies', function() {
 return gulp
        .src('src/GlobalAssemblyInfo.cs')
        .pipe($.change(function(content){
          return "using System.Reflection;\n\
//------------------------------------------------------------------------------\n\
// <auto-generated>\n\
//     This code was generated by a tool.\n\
//     Changes to this file may cause incorrect behavior and will be lost if\n\
//     the code is regenerated.\n\
// </auto-generated>\n\
//------------------------------------------------------------------------------\n\
[assembly: AssemblyVersion(\"" + versionNumber + "\")]\n\
[assembly: AssemblyFileVersion(\"" + versionNumber + "\")]\n\
[assembly: AssemblyCopyright(\"Copyright Skimur " + new Date().getYear() + "\")]\n\
[assembly: AssemblyProduct(\"Skimur\")]\n\
[assembly: AssemblyTrademark(\"Skimur\")]\n\
[assembly: AssemblyCompany(\"\")]\n\
[assembly: AssemblyConfiguration(\"" + buildConfiguration + "\")]\n\
[assembly: AssemblyInformationalVersion(\"" + buildConfiguration + "\")]"
        }))
        .pipe(gulp.dest('src'));
});

gulp.task('nuget-download', function(done) {
    if(fs.existsSync('nuget.exe')) {
        done();
        return;
    }
    request.get('http://nuget.org/nuget.exe')
        .pipe(fs.createWriteStream('nuget.exe'))
        .on('close', done);
});

gulp.task('nuget-restore', ['nuget-download'], function() {
	return gulp.src('**/*.sln', {read: false})
    .pipe($.shell([
      'nuget restore <%= (file.path) %>'
    ]))
});
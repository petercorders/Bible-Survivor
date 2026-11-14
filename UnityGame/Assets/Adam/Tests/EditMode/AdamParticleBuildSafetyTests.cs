using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

public sealed class AdamParticleBuildSafetyTests
{
    string root;
    [SetUp] public void Setup(){root=Path.Combine(Path.GetTempPath(),"adam-particle-build-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
    [TearDown] public void Cleanup(){Directory.Delete(root,true);}
    void Validate(string output,string run)=>Type.GetType("BibleSurvivor.Unity.Editor.AdamParticleEvidence, Adam.Editor",true).GetMethod("ValidateIsolation").Invoke(null,new object[]{root,output,run});
    void Reject(string output,string run){var error=Assert.Throws<TargetInvocationException>(()=>Validate(output,run));Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);}
    [Test] public void FreshMarkedOutputWithSafeRunSuffixValidatesWithoutCreatingAnything()
    {
        File.WriteAllText(Path.Combine(root,".adam-particles-qa"),"");var output=Path.Combine(root,"fresh.app");Validate(output,"nativea12345");Assert.IsFalse(Directory.Exists(output));Assert.IsFalse(File.Exists(output));
    }
    [Test] public void UnsafeSuffixCannotReuseOrEscapeTheQaIdentity()
    {
        File.WriteAllText(Path.Combine(root,".adam-particles-qa"),"");foreach(var run in new[]{null,"a","UPPERCASE","abcdef\n","a-bbbb","1abcde","../abcdef"})Reject(Path.Combine(root,"fresh.app"),run);
    }
    [Test] public void MarkerRelativePathWrongExtensionAndExistingOutputsAreRejected()
    {
        var output=Path.Combine(root,"fresh.app");Reject(output,"nativea12345");File.WriteAllText(Path.Combine(root,".adam-particles-qa"),"");Reject("relative.app","nativea12345");Reject(Path.Combine(root,"wrong.exe"),"nativea12345");Directory.CreateDirectory(output);Reject(output,"nativea12345");Directory.Delete(output);File.WriteAllText(output,"");Reject(output,"nativea12345");
    }
}

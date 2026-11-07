#if FPS_AUDIO_SMOKE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class AudioPlayerSmoke : MonoBehaviour
{
    readonly List<string> results=new(),errors=new();
    string output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch(){DontDestroyOnLoad(new GameObject("Audio verification").AddComponent<AudioPlayerSmoke>());}
    void Awake()
    {
        output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../AudioReview/Native"));Directory.CreateDirectory(output);
        if(File.Exists(output+"/complete.txt"))File.Delete(output+"/complete.txt");
        File.WriteAllText(output+"/levels.tsv","capture\tpeak\trms\tsamples\n");
        Application.logMessageReceived+=(m,t,type)=>{if(type==LogType.Error||type==LogType.Exception)errors.Add(m+"\n"+t);};
    }
    void Check(bool pass,string name){results.Add((pass?"PASS ":"FAIL ")+name);File.WriteAllLines(output+"/results.txt",results);}
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.8f);
        var scenario=AudioRegressionScenario.Run(Check,output,true);
        while(true)
        {
            bool more;
            try{more=scenario.MoveNext();}catch(Exception e){errors.Add(e.ToString());Check(false,"Native audio scenario completes");break;}
            if(!more)break;yield return new WaitForSecondsRealtime((float)(double)scenario.Current);
        }
        (scenario as IDisposable)?.Dispose();Check(errors.Count==0,"No native runtime exceptions");
        File.WriteAllLines(output+"/errors.txt",errors);File.WriteAllText(output+"/complete.txt",DateTime.Now.ToString("O"));
        Application.Quit();
    }
}
#endif

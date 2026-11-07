#if UNITY_EDITOR || FPS_AUDIO_SMOKE
using System;
using System.IO;
using UnityEngine;

// Test-only post-listener capture. The audio thread writes into a bounded buffer;
// disk I/O and Unity API calls remain on the main thread.
public sealed class AudioOutputProbe : MonoBehaviour
{
    readonly object gate=new();
    float[] samples;
    int written,channels=2,rate;
    public void Begin(float seconds=6)
    { lock(gate){rate=AudioSettings.outputSampleRate;samples=new float[(int)(seconds*rate*8)];written=0;} }
    void OnAudioFilterRead(float[] data,int channelCount)
    {
        lock(gate)
        {
            if(samples==null)return;channels=channelCount;
            int length=Math.Min(data.Length,samples.Length-written);
            Array.Copy(data,0,samples,written,length);written+=length;
        }
    }
    public (float peak,double rms,int count) Save(string file)
    {
        float[] copy;int count,channelCount;
        lock(gate){count=written;channelCount=channels;copy=new float[count];if(count>0)Array.Copy(samples,copy,count);samples=null;}
        float peak=0;double square=0;
        foreach(float sample in copy){peak=Math.Max(peak,Math.Abs(sample));square+=sample*sample;}
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        using(var writer=new BinaryWriter(File.Create(file)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)channelCount);
            writer.Write(rate);writer.Write(rate*channelCount*2);writer.Write((short)(channelCount*2));writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            foreach(float sample in copy)writer.Write((short)Math.Clamp(sample*32767,-32768,32767));
        }
        return (peak,Math.Sqrt(square/Math.Max(1,count)),count);
    }
}
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using UnityEngine;
using Newtonsoft.Json.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine.Networking;
using Task = System.Threading.Tasks.Task;


public class QuizToSpeech : MonoBehaviour
{
    /// <summary>
    /// https://texttospeech.ru/ - register here and get token------------
    /// https://texttospeech.ru/docs - you can choose a voice code here   |
    ///                                                                   |
    /// </summary>							  |							
    [SerializeField] private string _token = "past token here"; //<========

    [SerializeField] private string _text;
    [SerializeField] private string _voice;
    private WWW _www;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private string _nameOfFile;
    [SerializeField] private AudioClip _currentClip;


    [ContextMenu("Request")]
    public async void Run()
    {
        await RequestClip();
    }

    private async Task RequestClip()
    {
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://texttospeech.ru/api"))
            {
                var contentList = new List<string>();
                contentList.Add($"key={Uri.EscapeDataString(_token)}");
                contentList.Add($"text={Uri.EscapeDataString(_text)}");
                contentList.Add($"voice={Uri.EscapeDataString(_voice)}");
                contentList.Add($"pitch={Uri.EscapeDataString("1.0")}");
                contentList.Add($"rate={Uri.EscapeDataString("1.0")}");
                contentList.Add($"volume={Uri.EscapeDataString("1.0")}");
                contentList.Add($"hertz={Uri.EscapeDataString("24050")}");
                contentList.Add($"format={Uri.EscapeDataString("wav")}");
                request.Content = new StringContent(string.Join("&", contentList));
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                var response = await httpClient.SendAsync(request);
                var context = response.Content.ReadAsStringAsync().Result;
                JObject jObj = JObject.Parse(context);
                var urlClip = jObj["file"]?.ToString();
                Debug.Log(urlClip);
                StartCoroutine(LoadAudioFromServer(urlClip, clip =>
                {

                    SavWav.Save(Application.dataPath+ "/Audio" + $"/{name}.wav", clip); // if you want to save the file

#if UNITY_EDITOR
                    AssetDatabase.Refresh();
#endif
                }));

                StartCoroutine(GetAudioClip(clip =>
                {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                }));
            }
        }



        IEnumerator LoadAudioFromServer(string url, Action<AudioClip> response)
        {
            var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);

            yield return request.SendWebRequest();

            if (!request.isHttpError && !request.isNetworkError)
            {
                response(DownloadHandlerAudioClip.GetContent(request));
            }
            else
            {
                Debug.LogErrorFormat("error request [{0}, {1}]", url, request.error);

                response(null);
            }

            request.Dispose();
        }
        
        IEnumerator GetAudioClip(Action<AudioClip> response)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(Application.dataPath+ "/Audio" + $"/{_nameOfFile}.wav", AudioType.WAV))
            {
                yield return www.SendWebRequest();
 
                if (www.isHttpError || www.isNetworkError)
                {
                    Debug.Log(www.error);
                    response(null);
                }
                else
                {
                    response(DownloadHandlerAudioClip.GetContent(www));
                }
            }
        }
    }
}



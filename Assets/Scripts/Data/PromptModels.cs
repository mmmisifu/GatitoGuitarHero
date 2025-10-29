using System;
using System.Collections.Generic;

[Serializable] public class Prompt { public float time; public int lane; public string lyric; }
[Serializable]
public class PromptFile
{
    // For reading, prompts from JSON file
    public List<Prompt> prompts = new();
    public float bpm;
    public float songOffset;   
}

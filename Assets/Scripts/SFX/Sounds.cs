using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Sounds
{ 
    public enum Sound 
    {
        // PLAYER //
        PlayerSlide,
        Player_Jump1,
        Player_Jump2,
        Player_Death_Fall,
        Player_Death_Burn,
        Player_Death_Grunt1,
        Player_Death_Grunt2,
        
        // OBSTACLES //

        // Sweeping Spikes
        SweepingSpikes_Hit1,
        SweepingSpikes_Hit2,
        SweepingSpikes_Open,
        SweepingSpikes_Close,
        SweepingSpikes_Swing,

        // INTERACTABLES //
        Door_Open,
        Door_Close,
        SlidingDoor_Open,
        SlidingDoor_Close,
        SteamVent_PressurePlate,
        Item_Pickup,

        // MISC
        Win_Sound,
        Lose_Sound
    }

    private static Dictionary<Sound, float> soundTimerDictionary;
    private static GameObject oneShotGameObject;
    private static AudioSource oneShotAudioSource;
    private static AudioSource[] allAudioSources;
    private static bool active;

    public static void Initialize()
    {
        soundTimerDictionary = new Dictionary<Sound, float>();
        allAudioSources = Resources.FindObjectsOfTypeAll(typeof(AudioSource)) as AudioSource[];
        active = true;
    }

    public static void PlaySound(Sound sound)
    {
        if (CanPlaySound(sound)) {
            if (oneShotGameObject == null) {
                oneShotGameObject = new GameObject("One Shot Sound");
                oneShotAudioSource = oneShotGameObject.AddComponent<AudioSource>();
                oneShotAudioSource.outputAudioMixerGroup = SoundManager.GetSoundManager().sfxVolMixer;
            }
            if (active || (sound == Sound.Win_Sound || sound == Sound.Lose_Sound)) {
                oneShotAudioSource.PlayOneShot(GetAudioClip(sound));
            }
        }
    }

    private static bool CanPlaySound(Sound sound)
    {
        switch(sound) {
        default:
            return true;
        }
    }

    private static AudioClip GetAudioClip(Sound sound)
    {
        foreach (SoundManager.SoundAudioClip soundAudioClip in SoundManager.GetSoundManager().soundAudioClipArray) {
            if (soundAudioClip.sound == sound) {
                return soundAudioClip.audioClip;
            }
        }
        Debug.LogError("Sound " + sound + " not found!");
        return null;
    }

    public static void StopAllAudio()
    {
        foreach (AudioSource audioS in allAudioSources) {
            audioS.Stop();
            active = false;
        }
    }
}
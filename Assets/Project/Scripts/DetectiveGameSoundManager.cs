using UnityEngine;
using System.Collections;

namespace Eduzo.Games.DetectiveGame
{
    public class DetectiveGameSoundManager : MonoBehaviour
    {
        public static DetectiveGameSoundManager instance;

        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource engineSource;

        [Header("Global Clips")]
        public AudioClip bgMusic;
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip buttonClickClip;

        [Header("Game End Clips")]
        public AudioClip winSound;
        public AudioClip loseSound;

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            if (bgMusic != null)
            {
                musicSource.clip = bgMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        public void StopEngine() => engineSource?.Stop();

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        public void PlayCorrect() => PlaySfx(correctClip);
        public void PlayWrong() => PlaySfx(wrongClip);
        public void PlayButtonClick() => PlaySfx(buttonClickClip);

        public void PlayWin() => StartCoroutine(PlayEndSequence(winSound));
        public void PlayLose() => StartCoroutine(PlayEndSequence(loseSound));

        private IEnumerator PlayEndSequence(AudioClip clip)
        {
            if (clip == null) yield break;

            if (musicSource.isPlaying) musicSource.Pause();
            if (engineSource != null && engineSource.isPlaying) engineSource.Pause();

            sfxSource.Stop();
            sfxSource.PlayOneShot(clip);

            yield return new WaitForSeconds(clip.length);

            musicSource.UnPause();
            if (engineSource != null) engineSource.UnPause();
        }
    }
}

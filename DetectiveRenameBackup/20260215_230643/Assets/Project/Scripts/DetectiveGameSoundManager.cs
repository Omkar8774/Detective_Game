using UnityEngine;
using System.Collections;

namespace Eduzo.Games.DetectiveGame
{
    public class CarTollSoundManager : MonoBehaviour
    {
        public static CarTollSoundManager instance;

        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource engineSource;

        [Header("Clips")]
        public AudioClip bgMusic;
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip winSound;

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

        public void StopEngine() => engineSource.Stop();

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
                sfxSource.PlayOneShot(clip);
        }

        public void PlayCorrect() => PlaySfx(correctClip);

        public void PlayWrong() => PlaySfx(wrongClip);

        public void PlayWin()
        {
            StartCoroutine(PlayWinSequence());
        }

        private IEnumerator PlayWinSequence()
        {
            if (musicSource.isPlaying)
                musicSource.Pause();

            if (engineSource.isPlaying)
                engineSource.Pause();

            sfxSource.Stop();
            sfxSource.PlayOneShot(winSound);

            yield return new WaitForSeconds(winSound.length);

            musicSource.UnPause();
            engineSource.UnPause();
        }
    }
}
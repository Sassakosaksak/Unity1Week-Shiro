using System.Collections;
using UnityEngine;

public class GameAudioController : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip titleBgm;
    [SerializeField, Range(0f, 1f)] private float titleBgmVolume = 0.5f;
    [SerializeField] private AudioClip dialogueBgm;
    [SerializeField, Range(0f, 1f)] private float dialogueBgmVolume = 0.5f;
    [SerializeField] private AudioClip preparationBgm;
    [SerializeField, Range(0f, 1f)] private float preparationBgmVolume = 0.5f;
    [SerializeField] private AudioClip invasionBgm;
    [SerializeField, Range(0f, 1f)] private float invasionBgmVolume = 0.5f;
    [SerializeField] private AudioClip failureJingle;
    [SerializeField, Range(0f, 1f)] private float failureJingleVolume = 0.7f;
    [SerializeField] private AudioClip successJingle;
    [SerializeField, Range(0f, 1f)] private float successJingleVolume = 0.7f;
    [SerializeField] private AudioClip victoryAmbientBgm;
    [SerializeField, Range(0f, 1f)] private float victoryAmbientBgmVolume = 0.35f;
    [SerializeField] private AudioClip endingJingle;
    [SerializeField, Range(0f, 1f)] private float endingJingleVolume = 0.7f;

    private Coroutine victoryAmbientCoroutine;
    private Coroutine endingDialogueCoroutine;

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = GameController.Instance;
        }

        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        PlayPhaseBgm(gameController != null ? gameController.CurrentPhase : GameController.GamePhase.Preparation);
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopMusic();
    }

    private void Subscribe()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.PhaseChanged -= PlayPhaseBgm;
        gameController.PhaseChanged += PlayPhaseBgm;
        gameController.ResultShown -= PlayResultMusic;
        gameController.ResultShown += PlayResultMusic;
    }

    private void Unsubscribe()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.PhaseChanged -= PlayPhaseBgm;
        gameController.ResultShown -= PlayResultMusic;
    }

    private void PlayPhaseBgm(GameController.GamePhase phase)
    {
        if (phase == GameController.GamePhase.Title)
        {
            PlayLoopingMusic(titleBgm, titleBgmVolume);
            return;
        }

        if (phase == GameController.GamePhase.Opening)
        {
            PlayLoopingMusic(dialogueBgm, dialogueBgmVolume);
            return;
        }

        if (phase == GameController.GamePhase.Preparation)
        {
            PlayLoopingMusic(preparationBgm, preparationBgmVolume);
            return;
        }

        if (phase == GameController.GamePhase.Invasion)
        {
            PlayLoopingMusic(invasionBgm, invasionBgmVolume);
            return;
        }

        if (phase == GameController.GamePhase.Ending)
        {
            PlayEndingMusic();
            return;
        }

        StopMusic();
    }

    private void PlayResultMusic(GameController.GameResult result)
    {
        if (result == GameController.GameResult.Success)
        {
            PlaySuccessMusic();
            return;
        }

        PlayFailureMusic();
    }

    private void PlaySuccessMusic()
    {
        StopMusic();
        if (bgmSource == null)
        {
            return;
        }

        if (successJingle == null)
        {
            PlayLoopingMusic(victoryAmbientBgm, victoryAmbientBgmVolume);
            return;
        }

        bgmSource.PlayOneShot(successJingle, successJingleVolume);
        victoryAmbientCoroutine = StartCoroutine(PlayVictoryAmbientAfterJingle(successJingle.length));
    }

    private void PlayFailureMusic()
    {
        StopMusic();
        if (bgmSource != null && failureJingle != null)
        {
            bgmSource.PlayOneShot(failureJingle, failureJingleVolume);
        }
    }

    private void PlayEndingMusic()
    {
        StopMusic();
        if (bgmSource == null)
        {
            return;
        }

        if (endingJingle == null)
        {
            PlayLoopingMusic(dialogueBgm, dialogueBgmVolume);
            return;
        }

        bgmSource.PlayOneShot(endingJingle, endingJingleVolume);
        endingDialogueCoroutine = StartCoroutine(PlayEndingDialogueAfterJingle(endingJingle.length));
    }

    private IEnumerator PlayVictoryAmbientAfterJingle(float delay)
    {
        yield return new WaitForSeconds(delay);
        victoryAmbientCoroutine = null;

        if (gameController != null && gameController.CurrentPhase == GameController.GamePhase.Result)
        {
            PlayLoopingMusic(victoryAmbientBgm, victoryAmbientBgmVolume);
        }
    }

    private IEnumerator PlayEndingDialogueAfterJingle(float delay)
    {
        yield return new WaitForSeconds(delay);
        endingDialogueCoroutine = null;

        if (gameController != null && gameController.CurrentPhase == GameController.GamePhase.Ending)
        {
            PlayLoopingMusic(dialogueBgm, dialogueBgmVolume);
        }
    }

    private void PlayLoopingMusic(AudioClip clip, float volume)
    {
        StopMusic();
        if (clip == null || bgmSource == null)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    private void StopMusic()
    {
        if (victoryAmbientCoroutine != null)
        {
            StopCoroutine(victoryAmbientCoroutine);
            victoryAmbientCoroutine = null;
        }

        if (endingDialogueCoroutine != null)
        {
            StopCoroutine(endingDialogueCoroutine);
            endingDialogueCoroutine = null;
        }

        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
        bgmSource.loop = false;
    }
}

using Godot;

namespace Countdown.scripts;

public partial class SoundManager : Node
{
    [Export] private AudioStreamPlayer _countdownAudioStreamPlayer;
    [Export] private AudioStreamPlayer _lightsAudioStreamPlayer;
    [Export] private AudioStreamPlayer _player1AudioStreamPlayer;
    [Export] private AudioStreamPlayer _player2AudioStreamPlayer;

    [Export] private AudioStream _countdownActiveAudioStream;
    [Export] private AudioStream _countdownWarningAudioStream;
    
    [Export] private AudioStream _lightsOnAudioStream;
    [Export] private AudioStream _lightsOffAudioStream;
    
    [Export] private AudioStream _player1DeathAudioStream;
    [Export] private AudioStream _player2DeathAudioStream;

    public void PlayCountdownActiveAudioStream()
    {
        if (_countdownAudioStreamPlayer == null || _countdownActiveAudioStream == null) return;
        
        _countdownAudioStreamPlayer.Stop();
        _countdownAudioStreamPlayer.Stream = _countdownActiveAudioStream;
        _countdownAudioStreamPlayer.Play();
    }

    public void PlayCountdownWarningAudioStream()
    {
        if (_countdownAudioStreamPlayer == null || _countdownWarningAudioStream == null) return;
        
        _countdownAudioStreamPlayer.Stop();
        _countdownAudioStreamPlayer.Stream = _countdownWarningAudioStream;
        _countdownAudioStreamPlayer.Play();
    }

    public void PlayLightsOnAudioStream()
    {
        if (_lightsAudioStreamPlayer == null || _lightsOnAudioStream == null) return;

        _lightsAudioStreamPlayer.Stop();
        _lightsAudioStreamPlayer.Stream = _lightsOnAudioStream;
        _lightsAudioStreamPlayer.Play();
    }

    public void PlayLightsOffAudioStream()
    {
        if (_lightsAudioStreamPlayer == null || _lightsOffAudioStream == null) return;

        _lightsAudioStreamPlayer.Stop();
        _lightsAudioStreamPlayer.Stream = _lightsOffAudioStream;
        _lightsAudioStreamPlayer.Play();
    }

    public void PlayPlayer1DeathAudioStream()
    {
        if (_player1AudioStreamPlayer == null || _player1DeathAudioStream == null) return;

        _player1AudioStreamPlayer.Stop();
        _player1AudioStreamPlayer.Stream = _player1DeathAudioStream;
        _player1AudioStreamPlayer.Play();
    }

    public void PlayPlayer2DeathAudioStream()
    {
        if (_player2AudioStreamPlayer == null || _player2DeathAudioStream == null) return;
        
        _player2AudioStreamPlayer.Stop();
        _player2AudioStreamPlayer.Stream = _player2DeathAudioStream;
        _player2AudioStreamPlayer.Play();
    }
}

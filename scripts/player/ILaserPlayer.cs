public interface ILaserPlayer
{
	int PlayerId { get; }
	bool IsInvulnerable { get; }
	bool IsAlive { get; }
	void HitByLaser(int attackingPlayerId);
	void ResetPersonalCountdown();
}

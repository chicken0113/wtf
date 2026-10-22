using UnityEngine;

namespace EverythingGrows
{
    // Owns player health, immunity and action timers. The component keeps serialized settings.
    internal sealed class PlayerCombatState
    {
        private readonly FarmGame farm;
        private readonly OpenFieldCombat settings;
        private readonly CombatEffects effects;
        private float attackCooldown, invulnerability, rollTime, rollCooldown, quietTime;
        private float rollDurationAtStart, playerHitTime, rollBufferTime;
        private Vector2 rollDirection, bufferedRollDirection;
        private int removedHearts;

        public PlayerCombatState(FarmGame farm, OpenFieldCombat settings, CombatEffects effects)
        {
            this.farm = farm;
            this.settings = settings;
            this.effects = effects;
            Health = MaxHealth;
        }

        public int HeartCount => Mathf.Max(0, settings.StartingHearts - removedHearts);
        public int RemovedHearts => removedHearts;
        public float MaxHealth => HeartCount * settings.HealthPerHeart;
        public float HalfHeartHealth => settings.HealthPerHeart * .5f;
        public float Health { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsRolling => rollTime > 0;
        public float RollProgress => rollDurationAtStart > 0 ? Mathf.Clamp01(1 - rollTime / rollDurationAtStart) : 1;
        public Vector2 RollDirection => rollDirection;
        public float PlayerHitFlash => Mathf.Clamp01(playerHitTime / .12f);

        public float HeartFill(int index)
        {
            float filledHalves = Mathf.Floor(Health / HalfHeartHealth + .0001f);
            return Mathf.Clamp01((filledHalves - index * 2) * .5f);
        }

        public bool HarvestHeart(int index)
        {
            if (IsGameOver || index < 0 || index >= HeartCount) return false;
            var heart = Resources.Load<PlantDefinition>("Equipment/Heart");
            if (heart == null) { farm.Notify("Heart item data is missing."); return false; }
            if (!farm.ReceiveHarvest(heart)) return false;
            removedHearts++;
            Health = Mathf.Min(Health, MaxHealth);
            if (HeartCount == 0)
            {
                IsGameOver = true;
                rollTime = 0;
                farm.Shop?.Close();
                farm.Notify("Game over. You harvested your last heart.");
                return true;
            }
            farm.Notify("One heart harvested. Maximum HP decreased.");
            return true;
        }

        public void Restore(float health, int heartsRemoved, bool gameOver)
        {
            removedHearts = Mathf.Clamp(heartsRemoved, 0, settings.StartingHearts);
            Health = Mathf.Clamp(health, 0, MaxHealth);
            IsGameOver = gameOver || HeartCount == 0;
            invulnerability = 1;
            rollTime = rollBufferTime = 0;
        }

        public bool RestoreHeart()
        {
            if (IsGameOver) return false;
            if (removedHearts <= 0) { farm.Notify("All heart slots are already attached."); return false; }
            removedHearts--;
            Health = Mathf.Min(MaxHealth, Health + settings.HealthPerHeart);
            return true;
        }

        public bool Roll(Vector2 direction)
        {
            if (IsGameOver || IsRolling || rollCooldown > 0 || direction.sqrMagnitude < .01f) return false;
            rollDirection = direction.normalized;
            rollTime = farm.Controls != null ? Mathf.Max(.02f, farm.Controls.rollDuration) : .22f;
            rollDurationAtStart = rollTime;
            rollCooldown = farm.Controls != null ? Mathf.Max(0, farm.Controls.rollCooldown) : .65f;
            invulnerability = Mathf.Max(invulnerability, rollTime);
            rollBufferTime = 0;
            return true;
        }

        public bool RequestRoll(Vector2 direction)
        {
            if (IsGameOver || direction.sqrMagnitude < .01f) return false;
            if (Roll(direction)) return true;
            bufferedRollDirection = direction.normalized;
            rollBufferTime = farm.Controls != null ? Mathf.Clamp(farm.Controls.rollInputBuffer, 0, .3f) : .12f;
            return rollBufferTime > 0;
        }

        public bool TryBeginAttack(PlantDefinition weapon)
        {
            if (IsGameOver || weapon == null || weapon.effect != GrowthEffect.Weapon || attackCooldown > 0 || IsRolling) return false;
            attackCooldown = weapon.attackInterval;
            return true;
        }

        public void Damage(float damage)
        {
            if (IsGameOver || damage <= 0 || invulnerability > 0 || farm.IsSafe(farm.PlayerPosition)) return;
            float steppedDamage = Mathf.Ceil(damage / HalfHeartHealth) * HalfHeartHealth;
            Health = Mathf.Max(0, Health - steppedDamage);
            invulnerability = .65f;
            quietTime = 0;
            playerHitTime = .12f;
            farm.ImpactFeedback(true);
            effects.Sparks(farm.PlayerPosition, new Color(1, .4f, .3f));
            if (Health > 0) return;
            // Preserve solo recovery. Planted multiplayer resurrection is a separate milestone.
            farm.TeleportPlayer(farm.SpawnPosition);
            Health = MaxHealth;
            invulnerability = 1.5f;
            rollTime = rollBufferTime = 0;
            farm.Notify("You collapsed. Returned to the farm; your items are safe.");
        }

        public void Tick(float seconds)
        {
            attackCooldown = Mathf.Max(0, attackCooldown - seconds);
            invulnerability = Mathf.Max(0, invulnerability - seconds);
            playerHitTime = Mathf.Max(0, playerHitTime - seconds);
            float previousRollCooldown = rollCooldown;
            rollCooldown = Mathf.Max(0, rollCooldown - seconds);
            quietTime += seconds;
            if (rollBufferTime > 0)
            {
                bool ready = !IsRolling && previousRollCooldown <= Mathf.Min(seconds, rollBufferTime);
                rollBufferTime = Mathf.Max(0, rollBufferTime - seconds);
                if (ready) Roll(bufferedRollDirection);
            }
            if (rollTime > 0)
            {
                float active = Mathf.Min(rollTime, seconds);
                float speed = farm.Controls != null ? Mathf.Max(.1f, farm.Controls.rollSpeed) : 11;
                farm.MovePlayer(farm.PlayerPosition + rollDirection * active * speed);
                rollTime = Mathf.Max(0, rollTime - seconds);
            }
            if (farm.IsSafe(farm.PlayerPosition) && quietTime > 2)
                Health = Mathf.Min(MaxHealth, Health + seconds * 14);
        }
    }
}

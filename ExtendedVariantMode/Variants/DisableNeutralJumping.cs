﻿using Celeste;
using Celeste.Mod;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace ExtendedVariants.Variants {
    public class DisableNeutralJumping : AbstractExtendedVariant {
        public override int GetDefaultValue() {
            return 0;
        }

        public override int GetValue() {
            return Settings.DisableNeutralJumping ? 1 : 0;
        }

        public override void SetValue(int value) {
            Settings.DisableNeutralJumping = (value != 0);
        }

        public override void Load() {
            IL.Celeste.Player.WallJump += modWallJump;
        }

        public override void Unload() {
            IL.Celeste.Player.WallJump -= modWallJump;
        }

        
        private void modWallJump(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            // jump to the first MoveX usage (this.MoveX => ldarg.0 then ldfld MoveX basically)
            if(cursor.TryGotoNext(MoveType.Before,
                instr => instr.OpCode == OpCodes.Ldarg_0,
                instr => instr.MatchLdfld<Player>("moveX"))) {

                // sneak between the ldarg.0 and the ldfld (the ldarg.0 is the target to a jump instruction, so we should put ourselves after that.)
                cursor.Index++;

                ILCursor cursorAfterBranch = cursor.Clone();
                if(cursorAfterBranch.TryGotoNext(MoveType.After, instr => instr.OpCode == OpCodes.Brfalse_S)) {

                    Logger.Log("ExtendedVariantsModule", $"Inserting condition to enforce Disable Neutral Jumping at {cursor.Index} in CIL code for WallJump");

                    // pop the ldarg.0
                    cursor.Emit(OpCodes.Pop);

                    // before the MoveX check, check if neutral jumping is enabled: if it is not, skip the MoveX check
                    cursor.EmitDelegate<Func<bool>>(neutralJumpingEnabled);
                    cursor.Emit(OpCodes.Brfalse_S, cursorAfterBranch.Next);

                    // push the ldarg.0 again
                    cursor.Emit(OpCodes.Ldarg_0);
                }
            }
        }

        /// <summary>
        /// Indicates if neutral jumping is enabled.
        /// </summary>
        private bool neutralJumpingEnabled() {
            return !Settings.DisableNeutralJumping;
        }
    }
}
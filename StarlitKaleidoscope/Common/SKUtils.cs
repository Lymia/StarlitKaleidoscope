using System.Runtime.CompilerServices;
using XRL.World;
using XRL.World.Parts.Mutation;

namespace StarlitKaleidoscope.Common {
    public static class SKUtils {
        public static bool CheckRealityDistortion(BaseMutation mutation, Cell targetCell, IEvent E) {
            if (targetCell == mutation.ParentObject.CurrentCell) {
                if (!mutation.ParentObject.FireEvent(
                        Event.New("InitiateRealityDistortionLocal", "Object", mutation.ParentObject, "Mutation",
                            mutation), E))
                    return false;
            } else {
                Event E1 = Event.New("InitiateRealityDistortionTransit", "Object", mutation.ParentObject, "Mutation",
                    mutation, "Cell", targetCell);
                if (!mutation.ParentObject.FireEvent(E1, E) || !targetCell.FireEvent(E1, E))
                    return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidLoc(Zone zone, int x, int y) {
            return x >= 0 && x < zone.Width && (y >= 0 && y < zone.Height);
        }
    }
}
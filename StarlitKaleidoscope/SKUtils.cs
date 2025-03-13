using XRL.World;
using XRL.World.Parts.Mutation;

namespace StarlitKaleidoscope {
    public static class SKUtils {
        public static bool checkRealityDistortion(BaseMutation mutation, Cell targetCell, IEvent E) {
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
    }
}
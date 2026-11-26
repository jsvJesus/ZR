using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;

namespace Runtime.Characters
{
    public interface ICharacterOutfitStore
    {
        bool Exists(AssetPaths assets);

        CharacterOutfit? Read(AssetPaths assets);

        void Write(
            AssetPaths assets,
            CharacterOutfit outfit);
    }
}
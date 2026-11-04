package communication
{
   public class PlaySounds
   {
      internal static const SoundPlay:String = "play_sound";
      
      protected static var _sounds:Object = {
         "MOUSE_DOWN":"menu_click",
         "MOUSE_ENTER":"hover",
         "MOUSE_LEAVE":"out",
         "MOUSE_CLICK":"menu_click_up"
      };
      
      public function PlaySounds()
      {
         super();
      }
      
      public static function onOver() : *
      {
         onCall(_sounds.MOUSE_ENTER);
      }
      
      public static function onOut() : *
      {
         onCall(_sounds.MOUSE_LEAVE);
      }
      
      public static function onDown() : *
      {
         onCall(_sounds.MOUSE_DOWN);
      }
      
      public static function onClick() : *
      {
         onCall(_sounds.MOUSE_CLICK);
      }
      
      public static function onCall(sound:String) : *
      {
         var arr:Array = [sound];
         Api.call(SoundPlay,arr);
      }
   }
}


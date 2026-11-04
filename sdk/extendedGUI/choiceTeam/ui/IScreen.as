package ui
{
   public interface IScreen
   {
      function get screenID() : String;
      
      function set screenID(param1:String) : void;
      
      function get owner() : ScreenNavigator;
      
      function set owner(param1:ScreenNavigator) : void;
   }
}


package ui
{
   public interface IFocusDisplayObject
   {
      function get focusManager() : IFocusManager;
      
      function set focusManager(param1:IFocusManager) : void;
      
      function get isFocusEnabled() : Boolean;
      
      function set isFocusEnabled(param1:Boolean) : void;
      
      function get nextTabFocus() : IFocusDisplayObject;
      
      function set nextTabFocus(param1:IFocusDisplayObject) : void;
      
      function get previousTabFocus() : IFocusDisplayObject;
      
      function set previousTabFocus(param1:IFocusDisplayObject) : void;
      
      function showFocus() : void;
      
      function hideFocus() : void;
   }
}


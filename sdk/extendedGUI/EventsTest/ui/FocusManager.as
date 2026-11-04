package ui
{
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   
   public class FocusManager extends EventDispatcher
   {
      public function FocusManager(target:IEventDispatcher = null)
      {
         super(target);
      }
   }
}


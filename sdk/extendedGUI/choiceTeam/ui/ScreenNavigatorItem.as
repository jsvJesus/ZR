package ui
{
   import flash.display.DisplayObject;
   import flash.errors.IllegalOperationError;
   
   public class ScreenNavigatorItem
   {
      public var screen:Object;
      
      public var events:Object;
      
      public var properties:Object;
      
      public function ScreenNavigatorItem(screen:Object = null, events:Object = null, properties:Object = null)
      {
         super();
         this.screen = screen;
         this.events = Boolean(events) ? events : {};
         this.properties = Boolean(properties) ? properties : {};
      }
      
      internal function getScreen() : DisplayObject
      {
         var screenInstance:DisplayObject = null;
         var ScreenType:Class = null;
         var property:String = null;
         if(this.screen is Class)
         {
            ScreenType = Class(this.screen);
            screenInstance = new ScreenType();
         }
         else
         {
            if(!(this.screen is DisplayObject))
            {
               throw new IllegalOperationError("ScreenNavigatorItem \"screen\" must be a Class, a Function, or a Starling display object.");
            }
            screenInstance = DisplayObject(this.screen);
         }
         if(Boolean(this.properties))
         {
            for(property in this.properties)
            {
               screenInstance[property] = this.properties[property];
            }
         }
         return screenInstance;
      }
   }
}


package ui
{
   import flash.display.DisplayObject;
   import flash.errors.IllegalOperationError;
   import flash.events.Event;
   import flash.geom.Rectangle;
   
   public class ScreenNavigator extends UIControl
   {
      protected static var SIGNAL_TYPE:Class;
      
      public static const AUTO_SIZE_MODE_STAGE:String = "stage";
      
      public static const AUTO_SIZE_MODE_CONTENT:String = "content";
      
      private static const TRANSITION_START:String = "transition-start";
      
      protected var _autoSizeMode:String = "stage";
      
      protected var _activeScreenID:String;
      
      protected var _activeScreen:DisplayObject;
      
      protected var _clipContent:Boolean = false;
      
      public var transition:Function = defaultTransition;
      
      protected var _screens:Object = {};
      
      protected var _screenEvents:Object = {};
      
      protected var _transitionIsActive:Boolean = false;
      
      protected var _previousScreenInTransitionID:String;
      
      protected var _previousScreenInTransition:DisplayObject;
      
      protected var _nextScreenID:String = null;
      
      protected var _clearAfterTransition:Boolean = false;
      
      public function ScreenNavigator()
      {
         trace("new ScreenNavigator");
         this.addEventListener(Event.ADDED_TO_STAGE,this.screenNavigator_addedToStageHandler);
         this.addEventListener(Event.REMOVED_FROM_STAGE,this.screenNavigator_removedFromStageHandler);
         super();
      }
      
      protected static function defaultTransition(oldScreen:DisplayObject, newScreen:DisplayObject, completeCallback:Function) : void
      {
         completeCallback();
      }
      
      public function get autoSizeMode() : String
      {
         return this._autoSizeMode;
      }
      
      public function set autoSizeMode(value:String) : void
      {
         if(this._autoSizeMode == value)
         {
            return;
         }
         this._autoSizeMode = value;
         if(Boolean(this._activeScreen))
         {
            if(this._autoSizeMode == AUTO_SIZE_MODE_CONTENT)
            {
               this._activeScreen.addEventListener(Event.RESIZE,this.activeScreen_resizeHandler);
            }
            else
            {
               this._activeScreen.removeEventListener(Event.RESIZE,this.activeScreen_resizeHandler);
            }
         }
         this.invalidate(INVALIDATION_FLAG_SIZE);
      }
      
      public function get activeScreenID() : String
      {
         return this._activeScreenID;
      }
      
      public function get activeScreen() : DisplayObject
      {
         return this._activeScreen;
      }
      
      public function get clipContent() : Boolean
      {
         return this._clipContent;
      }
      
      public function set clipContent(value:Boolean) : void
      {
         if(this._clipContent == value)
         {
            return;
         }
         this._clipContent = value;
         this.invalidate(INVALIDATION_FLAG_STYLES);
      }
      
      protected function screenNavigator_addedToStageHandler(event:Event) : void
      {
         trace("screenNavigator_addedToStageHandler");
         this.stage.addEventListener(Event.RESIZE,this.stage_resizeHandler);
      }
      
      protected function screenNavigator_removedFromStageHandler(event:Event) : void
      {
         trace("screenNavigator_removedFromStageHandler");
         this.stage.removeEventListener(Event.RESIZE,this.stage_resizeHandler);
      }
      
      protected function stage_resizeHandler(event:Event) : void
      {
         trace("SCREENNAVI",this.stage.stageWidth,this.stage.stageHeight,this._activeScreen);
         this.resizeMyself();
      }
      
      protected function resizeMyself() : void
      {
         trace(this,"resizeMyself",this.stage.stageWidth,this.stage.stageHeight);
         this.width = this.stage.stageWidth;
         this.height = this.stage.stageHeight;
         this.resizeActiveScreen();
         if(this.clipContent)
         {
            this.scrollRect = new Rectangle(0,0,this.stage.stageWidth,this.stage.stageHeight);
         }
         else
         {
            this.scrollRect = null;
         }
      }
      
      protected function resizeActiveScreen() : void
      {
         trace("resizeActiveScreen..");
         try
         {
            if(this._activeScreen != null)
            {
               trace("resizeActiveScreen",this.actualWidth,this.actualHeight);
               if(this._activeScreen.width != this.actualWidth)
               {
                  this._activeScreen.width = this.actualWidth;
               }
               if(this._activeScreen.height != this.actualHeight)
               {
                  this._activeScreen.height = this.actualHeight;
               }
               if((this._activeScreen as UIControl).isInitialized)
               {
                  (this._activeScreen as UIControl).invalidate();
               }
            }
         }
         catch(error:Error)
         {
            trace("error catched while resize active screen:",error.getStackTrace());
         }
      }
      
      protected function activeScreen_resizeHandler(event:Event) : void
      {
         trace("activeScreen_resizeHandler");
      }
      
      protected function createScreenSignalListener(screenID:String, signal:Object) : Function
      {
         var self:ScreenNavigator = null;
         var signalListener:Function = null;
         self = this;
         if(signal.valueClasses.length == 1)
         {
            signalListener = function(arg0:Object):void
            {
               self.showScreen(screenID);
            };
         }
         else
         {
            signalListener = function(... rest):void
            {
               self.showScreen(screenID);
            };
         }
         return signalListener;
      }
      
      protected function createScreenEventListener(screenID:String) : Function
      {
         var self:ScreenNavigator = null;
         self = this;
         var eventListener:Function = function(event:Event):void
         {
            self.showScreen(screenID);
         };
         return eventListener;
      }
      
      public function clearScreen() : void
      {
         if(this._transitionIsActive)
         {
            this._nextScreenID = null;
            this._clearAfterTransition = true;
            return;
         }
         this.clearScreenInternal(true);
         this.dispatchEvent(new Event(Event.CLEAR));
      }
      
      protected function clearScreenInternal(displayTransition:Boolean) : void
      {
         var eventName:String = null;
         var signal:Object = null;
         var eventAction:Object = null;
         var eventListener:Function = null;
         if(!this._activeScreen)
         {
            return;
         }
         var item:ScreenNavigatorItem = ScreenNavigatorItem(this._screens[this._activeScreenID]);
         var events:Object = item.events;
         var savedScreenEvents:Object = this._screenEvents[this._activeScreenID];
         for(eventName in events)
         {
            signal = !!this._activeScreen.hasOwnProperty(eventName) ? this._activeScreen[eventName] as SIGNAL_TYPE : null;
            eventAction = events[eventName];
            if(eventAction is Function)
            {
               if(Boolean(signal))
               {
                  signal.remove(eventAction as Function);
               }
               else
               {
                  this._activeScreen.removeEventListener(eventName,eventAction as Function);
               }
            }
            else if(eventAction is String)
            {
               eventListener = savedScreenEvents[eventName] as Function;
               if(Boolean(signal))
               {
                  signal.remove(eventListener);
               }
               else
               {
                  this._activeScreen.removeEventListener(eventName,eventListener);
               }
            }
         }
         if(displayTransition)
         {
            this._transitionIsActive = true;
            this._previousScreenInTransition = this._activeScreen;
            this._previousScreenInTransitionID = this._activeScreenID;
         }
         this._screenEvents[this._activeScreenID] = null;
         this._activeScreen = null;
         this._activeScreenID = null;
         if(displayTransition)
         {
            this.transition(this._previousScreenInTransition,null,this.transitionComplete);
         }
         this.invalidate(INVALIDATION_FLAG_SELECTED);
      }
      
      public function addScreen(id:String, item:ScreenNavigatorItem) : void
      {
         if(this._screens.hasOwnProperty(id))
         {
            throw new IllegalOperationError("Screen with id \'" + id + "\' already defined. Cannot add two screens with the same id.");
         }
         this._screens[id] = item;
      }
      
      public function removeScreen(id:String) : void
      {
         if(!this._screens.hasOwnProperty(id))
         {
            throw new IllegalOperationError("Screen \'" + id + "\' cannot be removed because it has not been added.");
         }
         if(this._activeScreenID == id)
         {
            this.clearScreen();
         }
         delete this._screens[id];
      }
      
      public function showScreen(id:String) : DisplayObject
      {
         var eventName:String = null;
         var screen:IScreen = null;
         var signal:Object = null;
         var eventAction:Object = null;
         var eventListener:Function = null;
         this.resizeMyself();
         trace(this,"showScreen",id);
         if(!this._screens.hasOwnProperty(id))
         {
            throw new IllegalOperationError("Screen with id \'" + id + "\' cannot be shown because it has not been defined.");
         }
         if(this._transitionIsActive)
         {
            this._nextScreenID = id;
            this._clearAfterTransition = false;
            return null;
         }
         if(this._activeScreenID == id)
         {
            return this._activeScreen;
         }
         this._previousScreenInTransition = this._activeScreen;
         this._previousScreenInTransitionID = this._activeScreenID;
         if(Boolean(this._activeScreen))
         {
            this.clearScreenInternal(false);
         }
         this._transitionIsActive = true;
         var item:ScreenNavigatorItem = ScreenNavigatorItem(this._screens[id]);
         this._activeScreen = item.getScreen();
         if(this._activeScreen is IScreen)
         {
            screen = IScreen(this._activeScreen);
            screen.screenID = id;
            screen.owner = this;
            this.resizeActiveScreen();
         }
         this._activeScreenID = id;
         var events:Object = item.events;
         var savedScreenEvents:Object = {};
         for(eventName in events)
         {
            signal = !!this._activeScreen.hasOwnProperty(eventName) ? this._activeScreen[eventName] as SIGNAL_TYPE : null;
            eventAction = events[eventName];
            if(eventAction is Function)
            {
               if(Boolean(signal))
               {
                  signal.add(eventAction as Function);
               }
               else
               {
                  this._activeScreen.addEventListener(eventName,eventAction as Function);
               }
            }
            else
            {
               if(!(eventAction is String))
               {
                  throw new TypeError("Unknown event action defined for screen:",eventAction.toString());
               }
               if(Boolean(signal))
               {
                  eventListener = this.createScreenSignalListener(eventAction as String,signal);
                  signal.add(eventListener);
               }
               else
               {
                  eventListener = this.createScreenEventListener(eventAction as String);
                  this._activeScreen.addEventListener(eventName,eventListener);
               }
               savedScreenEvents[eventName] = eventListener;
            }
         }
         this._screenEvents[id] = savedScreenEvents;
         if(this._autoSizeMode == AUTO_SIZE_MODE_CONTENT || !this.stage)
         {
            this._activeScreen.addEventListener(Event.RESIZE,this.activeScreen_resizeHandler);
         }
         this.addChild(this._activeScreen);
         this.dispatchEvent(new Event(TRANSITION_START));
         this.transition(this._previousScreenInTransition,this._activeScreen,this.transitionComplete);
         this.dispatchEvent(new Event(Event.CHANGE));
         return this._activeScreen;
      }
      
      protected function transitionComplete() : void
      {
         trace("transitionComplete",this.activeScreen,this.activeScreen.width);
      }
   }
}


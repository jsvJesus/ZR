package logging
{
   import com.dvalimona.components.FontLoader;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.display.DisplayObjectContainer;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.filters.GlowFilter;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.ui.Keyboard;
   
   public class Logger extends Sprite
   {
      private static var _customChannels:Array;
      
      private static var tf:TextField;
      
      private static var _self:Logger;
      
      private static var _log:Vector.<LogObject>;
      
      private static var base:DisplayObjectContainer;
      
      public static var SHOW_CHANNELS:Boolean = true;
      
      public static const SWITCH_KEY_CODE:uint = Keyboard.G;
      
      public static var SWITCHED_ON:Boolean = true;
      
      private static const DEFAULT_ROTATION_Y:int = -16;
      
      private static const HIDED_ROTATION_Y:int = -90;
      
      public static const DEFAULT:String = "Default";
      
      public static const DEBUG:String = "Debug";
      
      public static const WARNING:String = "Warning";
      
      public static const ERROR:String = "Error";
      
      public static const FATAL_ERROR:String = "Fatal error";
      
      public static const LOCALIZATION:String = "Localization";
      
      public static const defaultChannels:Array = [DEFAULT,WARNING,ERROR,DEBUG,LOCALIZATION];
      
      public static var MAX_LINES:uint = 50;
      
      public static const SPLITTER:String = "\n";
      
      public function Logger()
      {
         super();
      }
      
      public static function get channels() : Array
      {
         if(Boolean(CustomChannels) && Boolean(CustomChannels.length))
         {
            return CustomChannels;
         }
         return defaultChannels;
      }
      
      public static function get CustomChannels() : Array
      {
         return _customChannels;
      }
      
      public static function set CustomChannels(value:Array) : void
      {
         _customChannels = value;
         printLog();
      }
      
      public static function ShowChannels(... args) : void
      {
         if(!args.length)
         {
            return;
         }
         if(!_customChannels)
         {
            _customChannels = new Array();
         }
         _customChannels = _customChannels.concat(args);
         printLog();
      }
      
      public static function ResetChannels() : void
      {
         _customChannels = null;
         printLog();
      }
      
      public static function get self() : Logger
      {
         if(!_self)
         {
            _self = new Logger();
         }
         return _self;
      }
      
      public static function set self(value:Logger) : void
      {
         _self = value;
      }
      
      private static function get log() : Vector.<LogObject>
      {
         if(!_log)
         {
            _log = new Vector.<LogObject>();
         }
         return _log;
      }
      
      private static function set log(value:Vector.<LogObject>) : void
      {
         _log = value;
      }
      
      public static function init(_base:DisplayObjectContainer, defaultShow:Boolean = false) : void
      {
         if(base == _base)
         {
            return;
         }
         initBase(_base);
         initKeyboardWorks(base);
         initTextField(defaultShow);
         addSelfToBase();
      }
      
      private static function initKeyboardWorks(_base:DisplayObjectContainer) : void
      {
         if(_base.stage != null)
         {
            _base.stage.addEventListener(KeyboardEvent.KEY_DOWN,onKeyDown);
         }
      }
      
      protected static function onKeyDown(event:KeyboardEvent) : void
      {
         trace(event.keyCode,event.keyCode == SWITCH_KEY_CODE);
         Logger.LogToChannel(Logger.DEBUG,"Key:",event.keyCode);
         if(event.keyCode == SWITCH_KEY_CODE)
         {
            doSwitchLogger();
         }
      }
      
      private static function doSwitchLogger(instant:Boolean = false) : void
      {
         trace("doSwitchLogger");
         TweenMax.killTweensOf(self);
         self.visible = true;
         TweenMax.fromTo(self,instant ? 0.01 : 0.9,{},{
            "alpha":(SWITCHED_ON ? 0 : 1),
            "rotationY":(SWITCHED_ON ? HIDED_ROTATION_Y : DEFAULT_ROTATION_Y),
            "ease":Expo.easeInOut,
            "onComplete":onSwitchCompleted
         });
         SWITCHED_ON = !SWITCHED_ON;
      }
      
      private static function onSwitchCompleted() : void
      {
         trace("SWITCHED_ON",SWITCHED_ON);
         self.visible = SWITCHED_ON;
      }
      
      private static function addSelfToBase() : void
      {
         (base as DisplayObjectContainer).addChild(self);
         self.mouseEnabled = false;
         self.mouseChildren = false;
      }
      
      private static function initBase(_base:DisplayObjectContainer) : void
      {
         base = _base;
         base.stage.addEventListener(Event.ADDED,onSomeToStageAdded);
         self.mouseEnabled = false;
      }
      
      protected static function onSomeToStageAdded(event:Event) : void
      {
         trace("========================================",event.target);
         bringMeToFront(event.target);
      }
      
      private static function bringMeToFront(so:*) : void
      {
         if(Boolean(self.parent))
         {
            self.parent.setChildIndex(self,self.parent.numChildren - 1);
         }
      }
      
      private static function initTextField(defaultShow:Boolean = false) : void
      {
         tf = new TextField();
         tf.autoSize = TextFieldAutoSize.LEFT;
         tf.defaultTextFormat = new TextFormat(FontLoader.getRegularFontName(),12,16777215,true);
         tf.text = "log";
         tf.selectable = false;
         if(StalkerBase.USE_FILTERS)
         {
            tf.filters = [new GlowFilter(0,1,4,4,3,1)];
         }
         tf.rotationY = DEFAULT_ROTATION_Y;
         if(!defaultShow)
         {
            doSwitchLogger(true);
         }
         self.addChild(tf);
      }
      
      public static function Log(... args) : void
      {
         var logObject:LogObject = new LogObject(args);
         log.push(logObject);
         printLog();
         trace("[" + logObject.channel + "]:",logObject);
      }
      
      public static function LogToChannel(channel:String = "Default", ... args) : void
      {
         var logObject:LogObject = new LogObject(args,channel);
         log.push(logObject);
         printLog();
         trace("[" + logObject.channel + "]:",logObject);
      }
      
      private static function printLog() : void
      {
         var temp:String = new String();
         for(var i:uint = Math.max(log.length - MAX_LINES,0); i < log.length; i++)
         {
            if(channels.indexOf(log[i].channel) >= 0)
            {
               if(log[i].channel != DEFAULT && SHOW_CHANNELS)
               {
                  temp += "[" + log[i].channel + "]: " + log[i] + SPLITTER;
               }
               else
               {
                  temp += log[i] + SPLITTER;
               }
            }
         }
         if(Boolean(tf))
         {
            tf.text = temp;
         }
      }
   }
}


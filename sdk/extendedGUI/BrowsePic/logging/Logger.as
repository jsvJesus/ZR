package logging
{
   import flash.display.DisplayObject;
   import flash.display.DisplayObjectContainer;
   import flash.display.Shape;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.KeyboardEvent;
   import flash.events.TextEvent;
   import flash.filters.GlowFilter;
   import flash.text.TextField;
   import flash.text.TextFieldAutoSize;
   import flash.text.TextFormat;
   import flash.ui.Keyboard;
   
   public class Logger extends Sprite
   {
      private static var _customChannels:Array;
      
      private static var tf:TextField;
      
      private static var bg:Shape;
      
      private static var _self:Logger;
      
      private static var _log:Vector.<LogObject>;
      
      private static var base:DisplayObjectContainer;
      
      public static var START_LINE:int = -1;
      
      public static var MAX_LINES:uint = 20;
      
      public static var MAX_SYMBOLS_PER_BIG_LINE:uint = 60;
      
      public static const SPLITTER:String = "\n";
      
      public static var SHOW_CHANNELS:Boolean = true;
      
      public static const SWITCH_KEY_CODE:uint = Keyboard.G;
      
      public static var SWITCHED_ON:Boolean = true;
      
      private static const DEFAULT_ROTATION_Y:int = 0;
      
      private static const HIDED_ROTATION_Y:int = -90;
      
      public static const DEFAULT:String = "Default";
      
      public static const DEBUG:String = "Debug";
      
      public static const TX:String = "tx";
      
      public static const RX:String = "rx";
      
      public static const WARNING:String = "Warning";
      
      public static const ERROR:String = "Error";
      
      public static const FATAL_ERROR:String = "Fatal error";
      
      public static const LOCALIZATION:String = "Localization";
      
      public static const EXTEND_FONT:String = "<font color=\'#e8e49f\' size=\'14\' face=\'Arial\'>";
      
      public static const DEFAULT_FONT:String = "<font color=\'#ffffff\' size=\'14\' face=\'Arial\'>";
      
      public static const DEBUG_FONT:String = "<font color=\'#00fcff\' size=\'14\' face=\'Arial\'>";
      
      public static const TX_FONT:String = "<font color=\'#70a0c9\' size=\'14\' face=\'Arial\'>";
      
      public static const RX_FONT:String = "<font color=\'#70c97d\' size=\'14\' face=\'Arial\'>";
      
      public static const WARNING_FONT:String = "<font color=\'#e8ea23\' size=\'14\' face=\'Arial\'>";
      
      public static const ERROR_FONT:String = "<font color=\'#ed3340\' size=\'14\' face=\'Arial\'>";
      
      public static const FATAL_ERROR_FONT:String = "<font color=\'#b9000d\' size=\'14\' face=\'Arial\'>";
      
      public static const LOCALIZATION_FONT:String = "<font color=\'#b2b2b2\' size=\'14\' face=\'Arial\'>";
      
      public static const defaultChannels:Array = [DEFAULT,WARNING,ERROR,DEBUG,LOCALIZATION,TX,RX];
      
      public static var use3d:Boolean = false;
      
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
         if(defaultShow)
         {
            addSelfToBase();
         }
      }
      
      public static function Hide() : void
      {
         removeSelfFromBase();
      }
      
      public static function Show() : void
      {
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
         if(event.keyCode == SWITCH_KEY_CODE)
         {
            doSwitchLogger();
         }
         switch(event.keyCode)
         {
            case SWITCH_KEY_CODE:
               doSwitchLogger();
               break;
            case Keyboard.UP:
               ScrollUp();
               break;
            case Keyboard.DOWN:
               ScrollDown();
               break;
            case Keyboard.HOME:
               ScrollHome();
               break;
            case Keyboard.END:
               ScrollEnd();
         }
      }
      
      public static function ScrollUp() : void
      {
         if(START_LINE < 0)
         {
            START_LINE = log.length - MAX_LINES - 1;
         }
         else
         {
            START_LINE -= 1;
         }
         if(START_LINE < 0)
         {
            START_LINE = 0;
         }
         printLog();
      }
      
      public static function ScrollDown() : void
      {
         if(START_LINE < 0)
         {
            START_LINE = log.length - MAX_LINES - 1;
         }
         else
         {
            START_LINE += 1;
         }
         if(START_LINE > log.length - MAX_LINES)
         {
            START_LINE = log.length - MAX_LINES;
         }
         printLog();
      }
      
      public static function ScrollHome() : void
      {
         START_LINE = 0;
         printLog();
      }
      
      public static function ScrollEnd() : void
      {
         START_LINE = -1;
         printLog();
      }
      
      public static function Switch() : void
      {
         doSwitchLogger();
      }
      
      private static function doSwitchLogger(instant:Boolean = false) : void
      {
         trace("doSwitchLogger",SWITCHED_ON);
         SWITCHED_ON = !SWITCHED_ON;
         if(Boolean(Base.logLevel))
         {
            Base.logLevel.visible = SWITCHED_ON;
         }
      }
      
      private static function onSwitchCompleted() : void
      {
         trace("SWITCHED_ON",SWITCHED_ON);
         self.visible = SWITCHED_ON;
         if(SWITCHED_ON && DEFAULT_ROTATION_Y == 0)
         {
         }
      }
      
      private static function fix3DBlur(di:DisplayObject) : void
      {
         di.scaleX = di.width / (di.width - 1);
         di.scaleY = di.height / (di.height - 1);
      }
      
      private static function addSelfToBase() : void
      {
         (base as DisplayObjectContainer).addChild(self);
      }
      
      private static function removeSelfFromBase() : void
      {
         if((base as DisplayObjectContainer).contains(self))
         {
            (base as DisplayObjectContainer).removeChild(self);
         }
      }
      
      private static function initBase(_base:DisplayObjectContainer) : void
      {
         base = _base;
         base.stage.addEventListener(Event.ADDED,onSomeToStageAdded);
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
         bg = new Shape();
         self.addChild(bg);
         tf = new TextField();
         tf.width = 600;
         tf.autoSize = TextFieldAutoSize.LEFT;
         tf.defaultTextFormat = new TextFormat("Arial",12,16777215,true);
         tf.selectable = false;
         tf.wordWrap = true;
         tf.borderColor = 13421772;
         tf.border = true;
         tf.addEventListener(TextEvent.LINK,linkClicked);
         if(Base.USE_FILTERS)
         {
            tf.filters = [new GlowFilter(0,1,4,4,3,1)];
         }
         if(use3d)
         {
            tf.rotationY = DEFAULT_ROTATION_Y;
         }
         if(!defaultShow)
         {
            doSwitchLogger(true);
         }
         self.addChild(tf);
         printLog();
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
      
      public static function clear(... args) : void
      {
         START_LINE = log.length;
         printLog();
      }
      
      private static function getFontByChannel(chl:String) : String
      {
         switch(chl)
         {
            case DEBUG:
               return DEBUG_FONT;
            case TX:
               return TX_FONT;
            case RX:
               return RX_FONT;
            case WARNING:
               return WARNING_FONT;
            case ERROR:
               return ERROR_FONT;
            case FATAL_ERROR:
               return FATAL_ERROR_FONT;
            case LOCALIZATION:
               return LOCALIZATION_FONT;
            default:
               return DEFAULT_FONT;
         }
      }
      
      private static function linkClicked(event:TextEvent) : void
      {
         Logger.LogToChannel(Logger.DEFAULT,event.text);
      }
      
      private static function printLog() : void
      {
         var string:String = null;
         var details:String = null;
         if(Boolean(self) && Boolean(self.stage))
         {
            self.x = self.stage.stageWidth * 0.5;
            self.y = 47;
         }
         var temp:String = new String();
         var startNum:uint = START_LINE >= 0 ? uint(START_LINE) : uint(Math.max(log.length - MAX_LINES,0));
         var endNum:uint = startNum + MAX_LINES;
         for(var i:uint = startNum; i < log.length; i++)
         {
            if(channels.indexOf(log[i].channel) >= 0)
            {
               if(SHOW_CHANNELS)
               {
                  string = i + ": " + log[i].toString();
                  if(string.length > MAX_SYMBOLS_PER_BIG_LINE && log[i].channel != DEFAULT)
                  {
                     string = string.substr(0,Math.min(MAX_SYMBOLS_PER_BIG_LINE,string.length - 1)) + " ... <a href=\'event:" + string + "\'>" + EXTEND_FONT + "<u>[more]</u>" + "</font>" + "</a>";
                  }
                  string = getFontByChannel(log[i].channel) + string + "</font>";
                  details = getFontByChannel(log[i].channel) + "<b>" + "[" + log[i].channel + "]: " + "</b></font>";
                  temp += details + string + SPLITTER;
               }
               else
               {
                  temp += log[i] + SPLITTER;
               }
            }
         }
         if(Boolean(tf))
         {
            tf.htmlText = temp;
            if(Boolean(self.stage))
            {
               tf.width = self.stage.stageWidth * 0.5;
               tf.height = self.stage.stageHeight - 47;
               bg.graphics.clear();
               bg.graphics.beginFill(0,0.7);
               bg.graphics.drawRect(0,0,tf.width,tf.height);
               bg.graphics.endFill();
            }
         }
      }
   }
}


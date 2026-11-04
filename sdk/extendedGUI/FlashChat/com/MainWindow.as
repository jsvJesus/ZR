package com
{
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.events.TextEvent;
   import flash.external.ExternalInterface;
   import flash.sampler.*;
   import flash.system.*;
   import flash.text.TextField;
   import flash.text.TextFieldType;
   import flash.text.TextFormat;
   import flash.ui.Keyboard;
   import flash.utils.getDefinitionByName;
   import flash.utils.getTimer;
   import flash.utils.setTimeout;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.controls.Button;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.data.DataProvider;
   import scaleform.clik.events.ButtonEvent;
   import scaleform.clik.events.ComponentEvent;
   import scaleform.clik.events.ListEvent;
   import scaleform.clik.events.ResizeEvent;
   import scaleform.clik.events.SliderEvent;
   import scaleform.clik.utils.ConstrainedElement;
   import scaleform.clik.utils.Constraints;
   import scaleform.clik.utils.Padding;
   
   public class MainWindow extends UIComponent
   {
      public var frame_thumb:Frame_Thumb;
      
      public var frame_thumb_down:Frame_Thumb;
      
      public var frame_thumb_right:Frame_Thumb;
      
      public var frame_thumb_up:Frame_Thumb;
      
      public var minBtn:minimizeButton;
      
      public var translateBtn:TranslBtn;
      
      internal var API:GameCommunication = new GameCommunication();
      
      internal var Parse:ParseChannels = new ParseChannels();
      
      internal var STR:StringParse = new StringParse();
      
      internal const MAX_LINES:* = 3;
      
      internal const MEMORY_LINES:* = 20;
      
      internal const MSG_TEXT_LIMIT:* = 22800;
      
      internal const MSG_HTML_LIMIT:* = 43000;
      
      internal const MSG_HTML_LIMIT_FULL:* = 63000;
      
      internal const USER_COLOR:* = "#0082ff";
      
      public const ACCOUNT_STATUS_ADM:* = 1;
      
      public var locale:Object = new Object();
      
      public var minWidth:Number = 200;
      
      public var maxWidth:Number = 700;
      
      public var minHeight:Number = 150;
      
      public var maxHeight:Number = 2000;
      
      public var ShowTime:Boolean = true;
      
      public var Scrolling:Boolean = true;
      
      public var ShowChan:Boolean = true;
      
      public var onlyEng:Boolean = false;
      
      public var HiddenMode:Boolean = false;
      
      public var MissClickMode:Boolean = false;
      
      public var mainMsgs:String = "";
      
      public var settings:Array = new Array();
      
      protected var haveMsg:Boolean = false;
      
      protected var selectedText:String = "";
      
      protected var tempString:String = "";
      
      public var inputMemory:Array = new Array();
      
      protected var currentLine:Number = -1;
      
      public var defaultChannals:Array = new Array();
      
      protected var _title:String;
      
      protected var _src:String = "";
      
      protected var _contentPadding:Padding;
      
      protected var _content:DisplayObject;
      
      protected var _dragProps:Array;
      
      protected var _dragPropsUP:Array;
      
      protected var settingsOpenFlag:Boolean = false;
      
      public var colorsOpenFlag:Boolean = false;
      
      public var _alphaGame:Number = 0.5;
      
      public var _alphaChat:Number = 1;
      
      protected var _fontSize:Number = 14;
      
      protected var _newWhispTabs:Boolean = false;
      
      protected var _newWhispTabsForFriend:Boolean = false;
      
      protected var _newWhispTabsForAll:Boolean = false;
      
      protected var _ctrl:Boolean;
      
      protected var _shift:Boolean;
      
      protected var _tmpInpt:String = "";
      
      protected var _status:Number = 0;
      
      protected var _command:Boolean = false;
      
      protected var _animation:Boolean = false;
      
      public var X:Number = x;
      
      public var Y:Number = y;
      
      protected var SH:Number;
      
      protected var radio_mode:Boolean = false;
      
      protected var _last_paste_time:Number = 0;
      
      public var closeBtn:Button;
      
      public var okBtn:Button;
      
      public var resizeBtn:Button;
      
      public var titleBtn:Button;
      
      public var OptBtn:Button;
      
      public var newBtn:Button;
      
      public var background:MovieClip;
      
      public var hit:MovieClip;
      
      public var txt:TextField;
      
      public var hiddenTxt:TextField;
      
      public var inpt:TextField;
      
      public var sb:DefaultScrollBar;
      
      public var TabBar:TabsBar;
      
      public var tf1:DefaultDropdownMenu;
      
      protected var Numlines:Number = 1;
      
      protected var defaultY:Number = 0;
      
      protected var defaultTxtHe:Number = 0;
      
      public var CommandsList:DefaultScrollingList = new DefaultScrollingList();
      
      public var intpHeight:Number = 0;
      
      public var whispID:Number;
      
      public var newsID:Number;
      
      public var ramka:ResizeFrame;
      
      public function MainWindow()
      {
         super();
         this._contentPadding = new Padding(30,0,0,0);
         hitArea = this.hit;
      }
      
      public function get title() : String
      {
         return this._title;
      }
      
      public function set title(value:String) : void
      {
         this._title = value;
         if(this.titleBtn.initialized)
         {
            this.titleBtn.label = this._title;
         }
      }
      
      public function set accountStatus(value:Number) : void
      {
         this._status = value;
      }
      
      public function get accountStatus() : Number
      {
         return this._status;
      }
      
      public function get source() : String
      {
         return this._src;
      }
      
      public function set source(value:String) : void
      {
         this._src = value;
         invalidate("source");
      }
      
      public function get alphaGame() : Number
      {
         return this._alphaGame;
      }
      
      public function set alphaGame(value:Number) : void
      {
         this._alphaGame = value;
      }
      
      public function get newWhispTabs() : Boolean
      {
         return this._newWhispTabs;
      }
      
      public function set newWhispTabs(value:Boolean) : *
      {
         this._newWhispTabs = value;
      }
      
      public function get newWhispTabsForAll() : Boolean
      {
         return this._newWhispTabsForAll;
      }
      
      public function set newWhispTabsForAll(value:Boolean) : *
      {
         this._newWhispTabsForAll = value;
      }
      
      public function get newWhispTabsForFriend() : Boolean
      {
         return this._newWhispTabsForFriend;
      }
      
      public function set newWhispTabsForFriend(value:Boolean) : *
      {
         this._newWhispTabsForFriend = value;
      }
      
      public function get alphaChat() : Number
      {
         return this._alphaChat;
      }
      
      public function set alphaChat(value:Number) : void
      {
         this._alphaChat = value;
      }
      
      public function get contentPadding() : Object
      {
         return this._contentPadding;
      }
      
      public function set contentPadding(value:Object) : void
      {
         this._contentPadding = new Padding(value.top,value.right,value.bottom,value.left);
         invalidate("padding");
      }
      
      public function get fontSize() : Number
      {
         return this._fontSize;
      }
      
      public function set fontSize(value:Number) : void
      {
         if(value == -1)
         {
            value = this._fontSize;
         }
         else
         {
            this._fontSize = value;
         }
         var newFormat:TextFormat = new TextFormat();
         newFormat.size = value;
         this.inpt.setTextFormat(newFormat);
         this.inpt.defaultTextFormat = newFormat;
         this.hiddenTxt.setTextFormat(newFormat);
         this.hiddenTxt.defaultTextFormat = newFormat;
         this.txt.setTextFormat(newFormat);
         this.txt.defaultTextFormat = newFormat;
         if(this.Scrolling)
         {
            this.ScrollMaxTxt();
         }
      }
      
      override protected function preInitialize() : void
      {
         constraints = new Constraints(this,ConstrainMode.REFLOW);
      }
      
      override protected function initialize() : void
      {
         tabEnabled = false;
         mouseEnabled = mouseChildren = enabled;
         super.initialize();
      }
      
      protected function ScrollMaxTxt() : *
      {
         var Fun:* = function():*
         {
            sb.position = txt.maxScrollV;
         };
         setTimeout(Fun,0);
      }
      
      public function EnterDown() : *
      {
         this.closeOptions();
         stage.focus = this.inpt;
         if(stage.focus != this.inpt)
         {
            stage.focus = Object(root).MainChat.inpt;
         }
      }
      
      protected function onFocusInput(e:KeyboardEvent) : *
      {
         if(e.keyCode == Keyboard.ENTER || e.keyCode == Keyboard.NUMPAD_ENTER)
         {
            this.EnterDown();
         }
      }
      
      override protected function configUI() : void
      {
         initSize();
         if(hitArea != null)
         {
            constraints.addElement("hitArea",hitArea,Constraints.ALL);
            constraints.addElement("sb",this.sb,Constraints.TOP | Constraints.LEFT | Constraints.BOTTOM);
            constraints.addElement("txt",this.txt,Constraints.ALL);
            constraints.addElement("hiddenTxt",this.hiddenTxt,Constraints.ALL);
            constraints.addElement("inpt",this.inpt,Constraints.BOTTOM | Constraints.RIGHT | Constraints.LEFT);
            constraints.addElement("tf1",this.tf1,Constraints.BOTTOM | Constraints.LEFT);
            constraints.addElement("translateBtn",this.translateBtn,Constraints.TOP | Constraints.RIGHT);
            this.inpt.addEventListener(FocusEvent.FOCUS_IN,this.addUser);
            this.inpt.addEventListener(FocusEvent.FOCUS_OUT,this.delUser);
            this.inpt.addEventListener(Event.CHANGE,this.inptChange);
            this.inpt.addEventListener(Event.CHANGE,this.onCommandChange);
            this.tf1.addEventListener(ListEvent.INDEX_CHANGE,this.setCommandChat);
         }
         if(this.background != null)
         {
            constraints.addElement("background",this.background,Constraints.ALL);
         }
         if(this.titleBtn != null)
         {
            this.titleBtn.label = this._title || "My Window";
            this.titleBtn.addEventListener(MouseEvent.MOUSE_DOWN,this.onWindowStartDrag,false,0,true);
            constraints.addElement("titleBtn",this.titleBtn,Constraints.TOP | Constraints.LEFT);
            constraints.addElement("TabBar",this.TabBar,Constraints.TOP | Constraints.RIGHT | Constraints.LEFT);
         }
         if(this.closeBtn != null)
         {
            this.closeBtn.addEventListener(MouseEvent.CLICK,this.onCloseButtonClick,false,0,true);
            constraints.addElement("closeBtn",this.closeBtn,Constraints.TOP | Constraints.RIGHT);
         }
         if(this.resizeBtn != null)
         {
            constraints.addElement("resizeBtn",this.resizeBtn,Constraints.BOTTOM | Constraints.RIGHT);
         }
         if(this.okBtn != null)
         {
            constraints.addElement("okBtn",this.okBtn,Constraints.BOTTOM | Constraints.RIGHT);
            this.okBtn.addEventListener(MouseEvent.CLICK,this.onCloseButtonClick,false,0,true);
         }
         constraints.addElement("minBtn",this.minBtn,Constraints.TOP | Constraints.RIGHT);
         this.minBtn.addEventListener(MouseEvent.CLICK,this.setHiddenChat);
         this.inpt.addEventListener(KeyboardEvent.KEY_DOWN,this.CtrlDown);
         this.inpt.addEventListener(KeyboardEvent.KEY_DOWN,this.getCommand);
         this.inpt.addEventListener(KeyboardEvent.KEY_DOWN,this.EnterDownChat);
         this.inpt.addEventListener(KeyboardEvent.KEY_UP,this.CtrlUp);
         this.inpt.addEventListener(Event.CHANGE,this.inptChange);
         this.inpt.addEventListener(FocusEvent.FOCUS_IN,this.inptFocusIn);
         this.inpt.addEventListener(FocusEvent.FOCUS_OUT,this.inptFocusOut);
         this.inpt.wordWrap = true;
         this.intpHeight = this.inpt.height;
         this.defaultY = this.inpt.y;
         this.defaultTxtHe = this.txt.height;
         this.AllMsg();
         this.OptBtn.addEventListener(MouseEvent.CLICK,this.clickOptions);
         addEventListener(FocusEvent.FOCUS_IN,this.focusIN);
         this.TabBar.addEventListener("SET_TAB",this.changeTab);
         this.newBtn.addEventListener(MouseEvent.CLICK,this.openNewTabWind);
         this.changeSetDropDown(-1);
         this.tf1.selectedIndex = 0;
         this.txt.mouseWheelEnabled = true;
         this.txt.addEventListener(TextEvent.LINK,this.drawContextMenu);
         this.txt.addEventListener(MouseEvent.MOUSE_UP,this.getSelectedText);
         this.inpt.backgroundColor = 1184274;
         this.inpt.background = true;
         addEventListener(MouseEvent.CLICK,this.focusOnInpt);
         addEventListener(MouseEvent.MIDDLE_MOUSE_DOWN,this.onWindowStartDrag,false,0,true);
         this.TabBar.addEventListener(TabBarEvent.RIGHT_CLICK,this.TabContextMenu);
         this.translateBtn.visible = false;
         this.CommandsList.itemRendererName = "ContextListItem";
         this.CommandsList.addEventListener(MouseEvent.MOUSE_DOWN,this.setCommandInList);
         this.unDrawCommandList();
         this.addChild(this.CommandsList);
         this.ramka.addEventListener(ResizeFrameEvent.RESIZE,this.onResizeWIndows);
         this.ramka.addEventListener(MouseEvent.MOUSE_DOWN,this.closeOptions);
         this.ramka.addEventListener("Move_Up_Start",this.Move_Up_Start);
         this.ramka.addEventListener("Move_Down_Start",this.Move_Down_Start);
         this.ramka.addEventListener("Move_Left_Start",this.Move_Left_Start);
         this.ramka.addEventListener("Move_Right_Start",this.Move_Right_Start);
         this.ramka.addEventListener("Move_Up_Stop",this.Move_Up_Stop);
         this.ramka.addEventListener("Move_Down_Stop",this.Move_Down_Stop);
         this.ramka.addEventListener("Move_Left_Stop",this.Move_Left_Stop);
         this.ramka.addEventListener("Move_Right_Stop",this.Move_Right_Stop);
         this.ramka.addEventListener("Move_Up",this.Move_Up_Move);
         this.ramka.addEventListener("Move_Down",this.Move_Down_Move);
         this.ramka.addEventListener("Move_Left",this.Move_Left_Move);
         this.ramka.addEventListener("Move_Right",this.Move_Right_Move);
         this.frame_thumb.visible = false;
         this.frame_thumb_down.visible = false;
         this.frame_thumb_up.visible = false;
         this.frame_thumb_right.visible = false;
      }
      
      private function sayShowColor(e:Event) : *
      {
         this.colorsOpenFlag = true;
      }
      
      private function sayHideColor(e:Event) : *
      {
         this.colorsOpenFlag = false;
      }
      
      protected function Move_Up_Start(e:Event) : *
      {
         this.frame_thumb_up.visible = true;
         this.frame_thumb_up.y = this.ramka.y;
         this.frame_thumb_up.x = mouseX;
      }
      
      protected function Move_Down_Start(e:Event) : *
      {
         this.frame_thumb_down.visible = true;
         this.frame_thumb_down.y = this.ramka.height + this.ramka.y;
         this.frame_thumb_down.x = mouseX;
      }
      
      protected function Move_Left_Start(e:Event) : *
      {
         this.frame_thumb.visible = true;
         this.frame_thumb.y = mouseY;
         this.frame_thumb.x = this.ramka.x;
      }
      
      protected function Move_Right_Start(e:Event) : *
      {
         this.frame_thumb_right.visible = true;
         this.frame_thumb_right.y = mouseY;
         this.frame_thumb_right.x = this.ramka.width + this.ramka.x;
      }
      
      protected function Move_Up_Stop(e:Event) : *
      {
         this.frame_thumb_up.visible = false;
         this.frame_thumb_up.y = 0;
         this.frame_thumb_up.x = 50;
      }
      
      protected function Move_Down_Stop(e:Event) : *
      {
         this.frame_thumb_down.visible = false;
         this.frame_thumb_down.y = 0;
         this.frame_thumb_down.x = 50;
      }
      
      protected function Move_Left_Stop(e:Event) : *
      {
         this.frame_thumb.visible = false;
         this.frame_thumb.y = 50;
         this.frame_thumb.x = 0;
      }
      
      protected function Move_Right_Stop(e:Event) : *
      {
         this.frame_thumb_right.visible = false;
         this.frame_thumb_right.y = 50;
         this.frame_thumb_right.x = 0;
      }
      
      protected function Move_Up_Move(e:Event) : *
      {
         this.frame_thumb_up.y = this.ramka.y;
         this.frame_thumb_up.x = mouseX;
      }
      
      protected function Move_Down_Move(e:Event) : *
      {
         this.frame_thumb_down.y = this.ramka.height + this.ramka.y;
         this.frame_thumb_down.x = mouseX;
      }
      
      protected function Move_Left_Move(e:Event) : *
      {
         this.frame_thumb.y = mouseY;
         this.frame_thumb.x = this.ramka.x;
      }
      
      protected function Move_Right_Move(e:Event) : *
      {
         this.frame_thumb_right.y = mouseY;
         this.frame_thumb_right.x = this.ramka.width + this.ramka.x;
      }
      
      public function initRamka() : *
      {
         var W:Number = this.width;
         var H:Number = this.height;
         this.ramka.width = W + 12;
         this.ramka.height = H + 12;
         setTimeout(this.TabBar.invalidateTabs,1);
      }
      
      internal function onResizeWIndows(e:*) : *
      {
         if(e as ResizeFrameEvent == null)
         {
            return;
         }
         this.frame_thumb_down.visible = false;
         this.frame_thumb_down.y = 0;
         this.frame_thumb_down.x = 50;
         this.frame_thumb.visible = false;
         this.frame_thumb.y = 50;
         this.frame_thumb.x = 0;
         this.frame_thumb_up.visible = false;
         this.frame_thumb_up.y = 0;
         this.frame_thumb_up.x = 50;
         this.frame_thumb_right.visible = false;
         this.frame_thumb_right.y = 50;
         this.frame_thumb_right.x = 0;
         this.height = (e as ResizeFrameEvent).height - 12;
         this.width = (e as ResizeFrameEvent).width - 12;
         this.x += (e as ResizeFrameEvent).x;
         this.y += (e as ResizeFrameEvent).y;
         this.X = this.x;
         this.Y = this.y;
         this.intpHeight = this.inpt.height;
         this.defaultY = this.inpt.y;
         this.defaultTxtHe = this.txt.height;
         this.X = x;
         this.Y = y;
         setTimeout(this.TabBar.invalidateTabs,1);
      }
      
      internal function hideTransBtn(e:FocusEvent) : *
      {
         this.translateBtn.visible = false;
         this.txt.removeEventListener(FocusEvent.FOCUS_OUT,this.hideTransBtn);
      }
      
      protected function getSelectedText(e:MouseEvent) : *
      {
         if(Object(root).language == "en")
         {
            if(this.txt.selectionEndIndex - this.txt.selectionBeginIndex > 0)
            {
               this.selectedText = this.txt.text.substring(this.txt.selectionBeginIndex,this.txt.selectionEndIndex);
               this.translateBtn.visible = true;
               this.translateBtn.addEventListener(MouseEvent.MOUSE_DOWN,this.startTranslate);
               this.txt.addEventListener(FocusEvent.FOCUS_OUT,this.hideTransBtn);
            }
            else
            {
               this.selectedText = "";
               this.translateBtn.visible = false;
               this.translateBtn.removeEventListener(MouseEvent.MOUSE_DOWN,this.startTranslate);
               this.txt.removeEventListener(FocusEvent.FOCUS_OUT,this.hideTransBtn);
            }
         }
      }
      
      protected function startTranslate(e:MouseEvent) : *
      {
         this.translateBtn.visible = false;
         this.translateBtn.removeEventListener(MouseEvent.MOUSE_DOWN,this.startTranslate);
         Object(root).Bing.createTranslate(this.selectedText);
         this.selectedText = "";
         Object(root).Bing.x = ((Object(root).width - stage.stageWidth) / 2 + stage.stageWidth) / 2 - this.width / 2;
         Object(root).Bing.y = ((Object(root).height - stage.stageHeight) / 2 + stage.stageHeight) / 2 - this.height / 2;
         Object(root).addChild(Object(root).Bing);
      }
      
      protected function setHiddenChat(e:MouseEvent) : *
      {
         setTimeout(Object(root).setMode,0,"hidden");
         this.minBtn.focused = 0;
         this.API.changeMode("hidden");
      }
      
      protected function TabContextMenu(event:TabBarEvent) : *
      {
         var handler:Function;
         var arr:Array = [this.locale.CONTEXT_NEW_TAB,this.locale.CONTEXT_DOUBLE_TAB,this.locale.CONTEXT_SETTINGS_TAB,this.locale.CONTEXT_DELETE_TAB];
         var Context:ContextTileList = new ContextTileList();
         Context.dataProvider = new DataProvider(arr);
         Context.x = mouseX;
         Context.y = mouseY;
         Context.height = 25.8 * arr.length;
         if(Context.y + Context.height + y > (Object(root).height + stage.stageHeight) / 2)
         {
            Context.y = (Object(root).height + stage.stageHeight) / 2 - y - Context.height;
         }
         if(Context.x + Context.width + x > (Object(root).width + stage.stageWidth) / 2)
         {
            Context.x = (Object(root).width + stage.stageWidth) / 2 - Context.width - x;
         }
         Context.itemRendererName = "ContextListItem";
         Context.name = "ContextMenu";
         handler = function(e:MouseEvent):void
         {
            delTabContextMenu(e,event.index);
         };
         Context.addEventListener(MouseEvent.CLICK,handler);
         Context.addEventListener(FocusEvent.FOCUS_OUT,this.delContextMenu1);
         addChild(Context);
         Context.scaleX = 1;
         stage.focus = Context;
      }
      
      protected function drawContextMenu(event:TextEvent) : *
      {
         var id:int;
         var arr:Array;
         var usr:String = null;
         var tmp:int = 0;
         var Context:ContextTileList = null;
         var handler:Function = null;
         if(this.inpt.type == TextFieldType.DYNAMIC)
         {
            return;
         }
         id = ExternalInterface.call("c++","TextField",this.txt,"urlMouseButton");
         usr = event.text;
         tmp = 0;
         arr = [this.locale.CONTEXT_SEND_MSG,this.locale.CONTEXT_INVITE,this.locale.CONTEXT_ADD_FRIEND,this.locale.CONTEXT_BAN,this.locale.CONTEXT_REPORT];
         if(usr.charAt(2) == " " && id == 2)
         {
            switch(usr.substr(0,2))
            {
               case "tx":
                  arr.push(this.locale.CONTEXT_TELEPORT);
                  usr = usr.substr(3);
                  tmp = 0;
                  break;
               case "iw":
                  arr.push(this.locale.CONTEXT_WEAPON);
                  usr = usr.substr(3);
                  tmp = 1;
                  break;
               default:
                  arr.push(this.locale.CONTEXT_WEATHER);
                  usr = usr.substr(3);
                  tmp = 2;
            }
         }
         switch(id)
         {
            case 1:
               this.addWhispMsg("@" + usr);
               break;
            case 2:
               Context = new ContextTileList();
               Context.dataProvider = new DataProvider(arr);
               Context.x = mouseX;
               Context.y = mouseY;
               Context.height = 25.8 * arr.length + 0.1;
               if(Context.y + Context.height + y > (Object(root).height + stage.stageHeight) / 2)
               {
                  Context.y = (Object(root).height + stage.stageHeight) / 2 - y - Context.height;
               }
               if(Context.x + Context.width + x > (Object(root).width + stage.stageWidth) / 2)
               {
                  Context.x = (Object(root).width + stage.stageWidth) / 2 - Context.width - x;
               }
               Context.itemRendererName = "ContextListItem";
               Context.name = "ContextMenu";
               handler = function(e:MouseEvent):void
               {
                  delContextMenu(e,usr,tmp);
               };
               Context.addEventListener(MouseEvent.CLICK,handler);
               Context.addEventListener(FocusEvent.FOCUS_OUT,this.delContextMenu1);
               addChild(Context);
               Context.scaleX = 1;
               stage.focus = Context;
         }
      }
      
      public function addWhispMsg(usr:String) : *
      {
         var number:Number = Number(this.findCommand(this.inpt.text)[0]);
         if(number == -1)
         {
            this.inpt.text = usr + " " + this.inpt.text;
         }
         else
         {
            this.inpt.text = usr + this.inpt.text.substr(number + 1,this.inpt.length);
         }
         this.inpt.setSelection(this.inpt.text.length,this.inpt.text.length);
         stage.focus = this.inpt;
         this._command = false;
         this.getMsgType();
      }
      
      protected function delContextMenu(e:MouseEvent, usr:String, flag:int = 0) : *
      {
         removeChild(getChildByName("ContextMenu"));
         switch(e.target.index)
         {
            case 0:
               this.addWhispMsg("@" + usr);
               break;
            case 1:
               this.API.sendCommand("invite",usr);
               break;
            case 2:
               this.API.sendCommand("add_friend",usr);
               break;
            case 3:
               this.API.sendCommand("ignore",usr);
               break;
            case 4:
               this.API.report(usr);
            case 5:
               if(flag == 2)
               {
                  this.API.sendCommand("weather",usr);
               }
               else if(flag == 1)
               {
                  this.API.sendCommand("item",usr);
               }
               else
               {
                  this.API.sendCommand("tx",usr);
               }
         }
      }
      
      protected function delTabContextMenu(e:MouseEvent, ind:Number) : *
      {
         removeChild(getChildByName("ContextMenu"));
         switch(e.target.index)
         {
            case 0:
               this.openNewTabWind(null);
               break;
            case 1:
               this.TabBar.copyTab(ind);
               break;
            case 2:
               this.TabBar.selectedIndex = ind;
               this.changeTab(null);
               this.closeOptions();
               this.openOptions();
               break;
            case 3:
               this.TabBar.deleteTab(ind);
         }
      }
      
      protected function delContextMenu1(e:FocusEvent) : *
      {
         removeChild(getChildByName("ContextMenu"));
      }
      
      protected function inptFocusIn(e:Event) : void
      {
         this.inptChange(e);
         this.inpt.textColor = this.tf1.color;
         if(this.tf1.curID == -1)
         {
            this.tf1.selectedIndex = 0;
         }
         this.updateRadioMode("inptFocusIn");
      }
      
      public function setHiddenSettings() : *
      {
         if(this.TabBar.selectedIndex < 0)
         {
            Object(root).setHiddenSettings(this.Parse.unparse(this.settings),this.fontSize,this.ShowChan,this.ShowTime);
         }
         else
         {
            Object(root).setHiddenSettings(this.Parse.unparse(this.TabBar.Tabs[this.TabBar.selectedIndex].setting),this.fontSize,this.ShowChan,this.ShowTime);
         }
      }
      
      protected function inptFocusOut(e:Event) : void
      {
         this.defaultSize();
         this.inpt.textColor = this.tf1.color;
         if(this.tf1.curID == -1)
         {
            this.tf1.selectedIndex = 0;
         }
         this.unDrawCommandList();
         if(this.radio_mode)
         {
            this.radio_mode = false;
            Object(root).set_radio_mode(this.radio_mode,this.name);
         }
      }
      
      protected function EnterDownChat(e:KeyboardEvent) : *
      {
         if(e.keyCode == Keyboard.ENTER || e.keyCode == Keyboard.NUMPAD_ENTER)
         {
            this.SendMsg();
            this.unDrawCommandList();
         }
      }
      
      protected function resetInputText() : *
      {
         this.inpt.text = this._tmpInpt;
      }
      
      protected function CtrlDown(e:KeyboardEvent) : *
      {
         var flag:* = false;
         if(e.keyCode == Keyboard.CONTROL)
         {
            this._ctrl = true;
            flag = getTimer() / 1000 - this._last_paste_time < 30;
            if(Object(root).MainChat.accountStatus < this.ACCOUNT_STATUS_ADM && flag)
            {
               this.inpt.type = TextFieldType.DYNAMIC;
            }
         }
         if(e.keyCode == Keyboard.UP)
         {
            this.DrawCommandUp();
         }
         if(e.keyCode == Keyboard.DOWN)
         {
            this.DrawCommandDown();
         }
         if(e.keyCode == Keyboard.LEFT && this._ctrl)
         {
            this.onInputLeft();
         }
         if(e.keyCode == Keyboard.RIGHT && this._ctrl)
         {
            this.onInputRight();
         }
         if(e.keyCode == Keyboard.V && this._ctrl)
         {
            if(getTimer() / 1000 - this._last_paste_time < 30)
            {
               this.API.pasteWarning();
               return;
            }
            this._last_paste_time = getTimer() / 1000;
         }
         if(e.keyCode == Keyboard.SHIFT)
         {
            this._shift = true;
         }
         if(this._shift && e.keyCode == Keyboard.INSERT)
         {
            this.inpt.text = "";
            this.inpt.type = TextFieldType.DYNAMIC;
         }
      }
      
      protected function onInputLeft() : *
      {
         if(this.inpt.caretIndex < 1)
         {
            return;
         }
         var tmp_string:String = this.inpt.text.substring(0,this.inpt.caretIndex);
         var list:Array = tmp_string.split(" ");
         var current_word:String = list[list.length - 1];
         var new_index:int = Math.max(0,this.inpt.caretIndex - current_word.length);
         this.inpt.setSelection(new_index,new_index);
      }
      
      protected function onInputRight() : *
      {
         if(this.inpt.caretIndex >= this.inpt.text.length)
         {
            return;
         }
         var tmp_string:String = this.inpt.text.substring(this.inpt.caretIndex,this.inpt.text.length);
         var list:Array = tmp_string.split(" ");
         var current_word:String = list[0];
         var new_index:int = Math.min(this.inpt.caretIndex + current_word.length,this.inpt.text.length);
         this.inpt.setSelection(new_index,new_index);
      }
      
      protected function DrawCommandUp() : *
      {
         if(this.inputMemory.length > 0)
         {
            if(this.tempString == "")
            {
               this.tempString == this.inpt.text.substring(0,this.inpt.text.length - 1);
            }
            ++this.currentLine;
            if(this.currentLine == this.inputMemory.length)
            {
               this.currentLine = -1;
            }
            if(this.currentLine == -1)
            {
               this.inpt.text = this.tempString;
               this.inpt.setSelection(this.inpt.text.length,this.inpt.text.length);
            }
            else
            {
               this.inpt.text = this.inputMemory[this.currentLine];
               this.inpt.setSelection(this.inpt.text.length,this.inpt.text.length);
            }
            this.getMsgType();
            this.inptChange(null);
            if(this.findComForDel(this.inpt.text)[1] != "null")
            {
               this._command = true;
            }
            else
            {
               this._command = false;
            }
         }
      }
      
      protected function DrawCommandDown() : *
      {
         if(this.inputMemory.length > 0)
         {
            if(this.tempString == "")
            {
               this.tempString == this.inpt.text.substring(0,this.inpt.text.length - 1);
            }
            --this.currentLine;
            if(this.currentLine == -2)
            {
               this.currentLine = this.inputMemory.length - 1;
            }
            if(this.currentLine == -1)
            {
               this.inpt.text = this.tempString;
               this.inpt.setSelection(this.inpt.text.length,this.inpt.text.length);
            }
            else
            {
               this.inpt.text = this.inputMemory[this.currentLine];
               this.inpt.setSelection(this.inpt.text.length,this.inpt.text.length);
            }
            this.getMsgType();
            this.inptChange(null);
            if(this.findComForDel(this.inpt.text)[1] != "null")
            {
               this._command = true;
            }
            else
            {
               this._command = false;
            }
         }
      }
      
      protected function SendMsg() : *
      {
         var usr:String = null;
         var comnd:String = null;
         var com_arr:Array = null;
         var j:* = undefined;
         var str:* = null;
         this.tempString = "";
         this.currentLine = -1;
         var id:Number = this.tf1.curID;
         var origin:String = this.inpt.text;
         var msg:String = this.inpt.text;
         if(this.STR.validateMessage(msg))
         {
            usr = "";
            comnd = "";
            if(id == -1)
            {
               this.tf1.selectedIndex = 0;
            }
            com_arr = this.findCommand(msg);
            if(com_arr[1].charAt(0) == "@")
            {
               usr = this.findCommand(msg)[1].substr(1);
               msg = msg.substr(this.findCommand(msg)[0]);
               msg = msg.substr(1);
            }
            if(this.findCommand(msg)[1].charAt(0) == "/")
            {
               comnd = this.findCommand(msg)[1].substr(1);
               msg = msg.substr(this.findCommand(msg)[0]);
               msg = msg.substr(1);
            }
            if(id == -99)
            {
               usr = this.TabBar.Tabs[this.TabBar.selectedIndex].user;
               for(j in Object(root).MainChat.defaultChannals)
               {
                  if(Object(root).MainChat.defaultChannals[j].label == Object(root).MainChat.locale.WHISPER)
                  {
                     id = Number(Object(root).MainChat.defaultChannals[j].id);
                     break;
                  }
               }
            }
            if(comnd != "")
            {
               comnd = this.STR.deleteSpaces(comnd);
               msg = this.STR.deleteSpaces(msg);
               this.API.sendCommand(comnd,msg);
               this.addMemory("/" + this.STR.replaceEnter(comnd) + " " + this.STR.replaceEnter(msg));
               this.defaultSize();
               this.inpt.text = "";
               this.inptChange(null);
               this.defaultSize();
               this.tf1.invalidateData();
               this.tf1.selectedIndex = this.tf1.selectedIndex;
               this.inpt.textColor = this.tf1.color;
            }
            else if(id == this.findWhispID() && (usr == "" || usr == "@" || this.STR.deleteSpaces(msg) == ""))
            {
               str = "<font size=\"" + this.fontSize + "\" color=\"#" + ChannelColors.getAt(this.settings[id].id).toString(16) + "\">";
               str = str + this.locale.BAD_USER_MSG + "</font>\n";
               this.txt.appendText(str);
               this.inpt.text = this.STR.replaceEnter(this.inpt.text);
               this.inptChange(null);
               this.defaultSize();
            }
            else
            {
               this.inpt.text = "";
               msg = this.STR.deleteSpaces(msg);
               this.API.sendMessage(id,usr,msg);
               this.addMemory("@" + this.STR.replaceEnter(usr) + " ");
               this.inpt.text = "";
               this.inptChange(null);
               this.defaultSize();
               this.tf1.invalidateData();
               this.tf1.selectedIndex = this.tf1.selectedIndex;
               this.inpt.textColor = this.tf1.color;
            }
         }
         this.inpt.text = "";
         this.inptChange(null);
         this.defaultSize();
         this.inptChange(null);
         this._command = false;
      }
      
      protected function addMemory(str:String) : *
      {
         var i:* = undefined;
         for(i in this.inputMemory)
         {
            if(this.inputMemory[i] == str)
            {
               this.inputMemory.splice(i,1);
            }
         }
         this.currentLine = -1;
         this.tempString = "";
         if(this.inputMemory.length == this.MEMORY_LINES)
         {
            this.inputMemory.pop();
         }
         this.inputMemory.unshift(str);
      }
      
      protected function getCommandInList(N:Number) : *
      {
         this.inpt.text = "/" + this.CommandsList.dataProvider[N] + this.inpt.text.substring(this.findComForDel(this.inpt.text)[0] + 1,this.inpt.length);
         this.inpt.setSelection(this.findComForDel(this.inpt.text)[0] + 1,this.findComForDel(this.inpt.text)[0] + 1);
      }
      
      protected function ArrowKeysDown(e:KeyboardEvent) : *
      {
         var tmp:int = 0;
         if(e.keyCode == Keyboard.UP)
         {
            tmp = this.CommandsList.selectedIndex;
            tmp--;
            if(tmp < 0)
            {
               tmp = int(this.CommandsList.dataProvider.length - 1);
            }
            this.getCommandInList(tmp);
            this.CommandsList.selectedIndex = tmp;
            this._command = true;
         }
         if(e.keyCode == Keyboard.DOWN)
         {
            tmp = this.CommandsList.selectedIndex;
            tmp++;
            if(tmp > this.CommandsList.dataProvider.length - 1 || tmp < 0)
            {
               tmp = 0;
            }
            this.getCommandInList(tmp);
            this.CommandsList.selectedIndex = tmp;
            this._command = true;
         }
      }
      
      protected function setCommandInList(e:MouseEvent) : *
      {
         this.inpt.text = "/" + e.target.label + this.inpt.text.substring(this.findComForDel(this.inpt.text)[0] + 1,this.inpt.length);
         this.inpt.setSelection(this.findComForDel(this.inpt.text)[0] + 2,this.findComForDel(this.inpt.text)[0] + 2);
         stage.focus = this.inpt;
      }
      
      protected function drawCommandList(data:Array) : *
      {
         if(data != null)
         {
            this.inpt.removeEventListener(KeyboardEvent.KEY_DOWN,this.CtrlDown);
            this.CommandsList.dataProvider = new DataProvider(data);
            if(data.length <= 4)
            {
               this.CommandsList.height = data.length * 25.8 + 1;
            }
            else
            {
               this.CommandsList.height = 4 * 25.8 + 1;
            }
            this.CommandsList.x = this.inpt.x;
            this.CommandsList.y = this.inpt.y - this.CommandsList.height;
            this.CommandsList.selectedIndex = -1;
            this.inpt.addEventListener(KeyboardEvent.KEY_DOWN,this.ArrowKeysDown);
            this.CommandsList.visible = true;
         }
         else
         {
            this.unDrawCommandList();
         }
      }
      
      protected function unDrawCommandList() : *
      {
         this.CommandsList.visible = false;
         this.inpt.addEventListener(KeyboardEvent.KEY_DOWN,this.CtrlDown);
         this.CommandsList.dataProvider = new DataProvider();
         this.CommandsList.height = this.CommandsList.dataProvider.length * 22;
         this.inpt.removeEventListener(KeyboardEvent.KEY_DOWN,this.ArrowKeysDown);
      }
      
      protected function onCommandChange(e:Event) : *
      {
         if(this.findComForDel(this.inpt.text)[0] + 1 >= this.inpt.caretIndex)
         {
            this.drawCommandList(Commands.getCommands(this.inpt.text));
         }
         else
         {
            this.drawCommandList(null);
         }
         if(this._command && this.findComForDel(this.inpt.text)[1] == "null")
         {
            this.inpt.text = "";
            this._command = false;
         }
         if(this.findComForDel(this.inpt.text)[1] == "null")
         {
            this._command = false;
         }
         else
         {
            this._command = true;
         }
      }
      
      protected function findComForDel(txt:String) : Array
      {
         var command:String = "";
         var comand:Boolean = false;
         for(var i:* = 0; i < txt.length; i++)
         {
            if(txt.charAt(i) == " ")
            {
               if(comand)
               {
                  return new Array(i - 1,command);
               }
            }
            else if(comand)
            {
               command += txt.charAt(i);
            }
            else
            {
               if(txt.charAt(i) != "/")
               {
                  return new Array(-1,"null");
               }
               comand = true;
            }
         }
         if(comand)
         {
            return new Array(txt.length - 1,command);
         }
         return new Array(-1,"null");
      }
      
      protected function CtrlUp(e:KeyboardEvent) : *
      {
         if(e.keyCode == Keyboard.CONTROL)
         {
            this._ctrl = false;
            this.inpt.type = TextFieldType.INPUT;
            stage.focus = this.inpt;
         }
         if(e.keyCode == Keyboard.SHIFT)
         {
            this._shift = false;
            if(!this._ctrl)
            {
               this.inpt.type = TextFieldType.INPUT;
            }
         }
      }
      
      protected function setCommandChat(e:Event) : *
      {
         stage.focus = this.inpt;
         if(this.tf1.curID == this.findWhispID())
         {
            if(this.inpt.text.charAt(0) != "@")
            {
               this.inpt.text = "@ " + this.inpt.text;
               this.inpt.setSelection(1,1);
            }
         }
         else if(this.inpt.text.charAt(0) == "@")
         {
            this.inpt.text = this.inpt.text.substr(1,this.inpt.length - 1);
            this.inpt.setSelection(this.inpt.selectionBeginIndex - 1,this.inpt.selectionBeginIndex - 1);
         }
         this.tf1.focused = 0;
      }
      
      protected function findCommand(txt:String) : Array
      {
         var command:String = "";
         var comand:Boolean = false;
         for(var i:* = 0; i < txt.length; i++)
         {
            if(txt.charAt(i) == " ")
            {
               if(comand)
               {
                  return new Array(i - 1,command);
               }
            }
            else if(comand)
            {
               if(txt.charAt(i) == "/" || txt.charAt(i) == "@")
               {
                  return new Array(-1,"err");
               }
               command += txt.charAt(i);
            }
            else
            {
               if(!(txt.charAt(i) == "/" || txt.charAt(i) == "@"))
               {
                  return new Array(-1,"null");
               }
               comand = true;
               command += txt.charAt(i);
            }
         }
         if(comand)
         {
            return new Array(txt.length - 1,command);
         }
         return new Array(-1,command);
      }
      
      protected function getMsgType() : Number
      {
         var s:* = undefined;
         var s1:String = null;
         var indx:* = undefined;
         var arr:Array = null;
         var i:* = undefined;
         var num:Number = -99;
         var Obj:Array = new Array();
         Obj = this.findCommand(this.inpt.text);
         s = Obj[1];
         num = Number(Obj[0]);
         switch(num)
         {
            case -1:
               if(this.inpt.text.length == 0)
               {
                  this.tf1.selectedIndex = 0;
                  this.inpt.textColor = this.tf1.color;
               }
               return -99;
            default:
               if(s.charAt(0) == "@")
               {
                  s1 = "@";
               }
               else
               {
                  s1 = s;
               }
               arr = new Array();
               if(this.TabBar.selectedIndex >= 0)
               {
                  arr = this.TabBar.Tabs[this.TabBar.selectedIndex].setting;
               }
               else
               {
                  arr = this.settings;
               }
               for(i = 0; i < arr.length; i++)
               {
                  if(arr[i].com == s1.toLocaleLowerCase())
                  {
                     this.tf1.changeLabel(arr[i].label,ChannelColors.getAt(arr[i].id));
                     setTimeout(this.tf1.changeLabel,10,arr[i].label,ChannelColors.getAt(arr[i].id));
                     indx = arr[i].id;
                     this.tf1.curID = indx;
                     if(s1 != "@")
                     {
                        this.inpt.text = this.inpt.text.substr(num);
                        this.inpt.text = this.inpt.text.substr(1);
                        this.inpt.setSelection(0,0);
                        stage.focus = this.inpt;
                     }
                     this.inpt.textColor = ChannelColors.getAt(arr[i].id);
                     return indx;
                  }
               }
               return -1;
         }
      }
      
      internal function getCommand(e:KeyboardEvent) : *
      {
         if(e.charCode == Keyboard.SPACE)
         {
            this.getMsgType();
         }
      }
      
      public function changeSetDropDown(val:Number) : *
      {
         var i:Number = NaN;
         var arr:Array = new Array();
         switch(val)
         {
            case -1:
               for(i = 0; i < this.settings.length; i++)
               {
                  if(Boolean(this.settings[i].selected) && this.settings[i].com != null)
                  {
                     arr.push({
                        "id":this.settings[i].id,
                        "label":this.settings[i].short_name || this.settings[i].label,
                        "com":this.settings[i].com,
                        "color":ChannelColors.getAt(this.settings[i].id)
                     });
                  }
               }
               break;
            default:
               for(i = 0; i < this.TabBar.Tabs[val].setting.length; i++)
               {
                  if(Boolean(this.TabBar.Tabs[val].setting[i].selected) && this.TabBar.Tabs[val].setting[i].com != null)
                  {
                     arr.push({
                        "id":this.TabBar.Tabs[val].setting[i].id,
                        "label":this.TabBar.Tabs[val].setting[i].short_name || this.TabBar.Tabs[val].setting[i].label,
                        "com":this.TabBar.Tabs[val].setting[i].com,
                        "color":ChannelColors.getAt(this.TabBar.Tabs[val].setting[i].id)
                     });
                  }
               }
         }
         this.tf1.dataArray = arr;
         this.tf1.selectedIndex = 0;
         this.inpt.textColor = this.tf1.color;
         try
         {
            if(this.tf1.dataArray.length == 0)
            {
               this.inpt.type = TextFieldType.DYNAMIC;
               this.tf1.visible = false;
               this.inpt.visible = false;
            }
            else
            {
               this.inpt.type = TextFieldType.INPUT;
               this.inpt.visible = true;
               this.tf1.visible = true;
            }
         }
         catch(e:Error)
         {
         }
         this.inpt.textColor = this.tf1.color;
         this.updateRadioMode("changeSetDropDown");
      }
      
      internal function changeTab(e:Event) : *
      {
         this.getMsgType();
         this.refreshTxt();
         stage.focus = this.inpt;
         if(this.settingsOpenFlag)
         {
            this.closeOptions();
            this.openOptions();
         }
         this.fontSize = this._fontSize;
         if(this.Scrolling)
         {
            this.ScrollMaxTxt();
         }
         this.getMsgType();
      }
      
      public function refreshTxt() : *
      {
         if(this.TabBar.selectedIndex > -1)
         {
            this.titleBtn.STATE = "";
            this.titleBtn.setState("up");
            this.txt.htmlText = this.TabBar.Tabs[this.TabBar.selectedIndex].TEXT;
            this.changeSetDropDown(this.TabBar.selectedIndex);
         }
         else
         {
            this.AllMsg();
         }
      }
      
      internal function inptChange(e:Event) : *
      {
         var N:int = this.inpt.numLines;
         if(N > 1 && N <= this.MAX_LINES)
         {
            this.inpt.y = this.defaultY - this.intpHeight * (N - 1);
            this.inpt.height = this.intpHeight * N;
            this.txt.height = this.hit.height - this.inpt.height - 10;
            this.sb.height = this.txt.height;
            this.tf1.height = this.inpt.height;
            this.tf1.y = this.inpt.y;
         }
         else if(N <= this.MAX_LINES)
         {
            this.defaultSize();
         }
         else
         {
            this.inpt.y = this.defaultY - this.intpHeight * (this.MAX_LINES - 1);
            this.inpt.height = this.intpHeight * this.MAX_LINES;
            this.txt.height = this.hit.height - this.inpt.height - 10;
            this.tf1.height = this.inpt.height;
            this.tf1.y = this.inpt.y;
            this.sb.height = this.txt.height;
         }
         this.updateRadioMode("inptChange");
      }
      
      internal function updateRadioMode(txt:String) : *
      {
         var is_radio_id:Boolean = this.isRadioChannel(this.tf1.curID) && this.inpt.visible && this.visible && stage.focus == this.inpt;
         if(this.radio_mode != is_radio_id)
         {
            this.radio_mode = is_radio_id;
            Object(root).set_radio_mode(this.radio_mode,this.name);
         }
      }
      
      internal function defaultSize() : *
      {
         this.inpt.y = this.defaultY;
         this.txt.height = this.defaultTxtHe;
         this.inpt.height = this.intpHeight;
         this.tf1.y = this.defaultY;
         this.tf1.height = this.intpHeight;
         this.sb.height = this.txt.height;
      }
      
      protected function openNewTabWind(e:MouseEvent) : *
      {
         this.closeOptions();
         this.colorsOpenFlag = true;
         Object(root).removeChild(Object(root).Bing);
         removeEventListener(MouseEvent.CLICK,this.focusOnInpt);
         this.settingsOpenFlag = true;
         var Opt:OptWind = new OptWind();
         Opt.y = -Opt.height - 10;
         Opt.x = 0;
         if(Opt.y + y < (Object(root).height - stage.stageHeight) / 2)
         {
            Opt.y = (Object(root).height - stage.stageHeight) / 2 - y;
         }
         if(Opt.x + x < (Object(root).width - stage.stageWidth) / 2)
         {
            Opt.x = (Object(root).width - stage.stageWidth) / 2 - x;
         }
         if(Opt.x + Opt.width + x > (Object(root).width + stage.stageWidth) / 2)
         {
            Opt.x = (Object(root).width + stage.stageWidth) / 2 - Opt.width - x;
         }
         Opt.name = "Settings";
         Opt.alpha = 1;
         Opt.type = "new";
         Opt.TabName.text = this.locale.TITLE_ADD;
         Opt.Title.text = this.locale.TITLE_ADD_SETTINGS;
         Opt.TitleChan.text = this.locale.CHANNELS_TITLE;
         Opt.LoadBtn.label = this.locale.ADD_BTN_TITLE;
         Opt.OnlyEng.label = this.locale.ONLY_ENG;
         Opt.newForm.whispTabChk.label = this.locale.WHISP_TAB_CHK;
         Opt.newForm.NickField.text = this.locale.NICK_FIELD;
         Opt.Chan.Channels.dataProvider = new DataProvider(this.defaultChannals);
         Opt.closeBtn.addEventListener(MouseEvent.CLICK,this.clickOptions);
         Opt.Colors.CloseBtn.label = this.locale.CLOSE;
         Opt.Colors.addEventListener("COLORS_SHOW",this.sayShowColor);
         Opt.Colors.addEventListener("COLORS_HIDE",this.sayHideColor);
         addChild(Opt);
         Opt.Colors.Draw(Object(root).colors);
      }
      
      protected function saveTitle(e:Event) : *
      {
         if(this.TabBar.selectedIndex < 0)
         {
            this.title = e.target.text;
         }
         else
         {
            this.TabBar.Tabs[this.TabBar.selectedIndex].label = e.target.text;
            if(this.TabBar.Tabs[this.TabBar.selectedIndex].label != Object(root).MainChat.locale[this.TabBar.Tabs[this.TabBar.selectedIndex].defaultTab])
            {
               this.TabBar.Tabs[this.TabBar.selectedIndex].defaultTab = "";
            }
            this.TabBar.changeDataArray(this.TabBar.selectedIndex,e.target.text);
         }
      }
      
      protected function openOptions() : *
      {
         if(Object(root).getChildByName("Bing") != null)
         {
            Object(root).removeChild(Object(root).Bing);
         }
         removeEventListener(MouseEvent.CLICK,this.focusOnInpt);
         this.settingsOpenFlag = true;
         this.defaultSize();
         var Opt:OptWind = new OptWind();
         Opt.y = -Opt.height - 10;
         Opt.x = 0;
         if(Opt.y + y < (Object(root).height - stage.stageHeight) / 2)
         {
            Opt.y = (Object(root).height - stage.stageHeight) / 2 - y;
         }
         if(Opt.x + x < (Object(root).width - stage.stageWidth) / 2)
         {
            Opt.x = (Object(root).width - stage.stageWidth) / 2 - x;
         }
         if(Opt.x + Opt.width + x > (Object(root).width + stage.stageWidth) / 2)
         {
            Opt.x = (Object(root).width + stage.stageWidth) / 2 - Opt.width - x;
         }
         Opt.Chan.Channels.addEventListener(ChannelEvent.CHANGE,this.changeChanalSettings);
         Opt.Chan.Channels.addEventListener(ChannelEvent.CHANGE_SOUND,this.changeSoundSettings);
         Opt.addEventListener("Color_change",this.changeChanalColor);
         Opt.name = "Settings";
         Opt.alpha = 1;
         Opt.closeBtn.addEventListener(MouseEvent.CLICK,this.clickOptions);
         Opt.BackChatSlider.value = this.alphaChat;
         Opt.BackChatSlider.addEventListener(SliderEvent.VALUE_CHANGE,this.sliderChatChange);
         Opt.BackGameSlider.value = this.alphaGame;
         Opt.BackGameSlider.addEventListener(SliderEvent.VALUE_CHANGE,this.sliderGameChange);
         Opt.FontSlider.value = this._fontSize;
         Opt.FontSlider.addEventListener(SliderEvent.VALUE_CHANGE,this.sliderFontChange);
         Opt.FontLabel.text = this.locale.FONT_SIZE_LABEL;
         Opt.Title.text = this.locale.TITLE_CHAT_SETTINGS;
         Opt.TitleWhisp.text = this.locale.WHISP_CHAT_SETTINGS_TITLE;
         Opt.Whisp1.label = this.locale.WHISP_CHAT_SETTING1;
         Opt.Whisp1.selected = this.newWhispTabs;
         Opt.Whisp1.addEventListener(Event.SELECT,this.changeNewWhispTabs);
         Opt.Whisp2.label = this.locale.WHISP_CHAT_SETTING2;
         Opt.Whisp2.selected = this.newWhispTabsForAll;
         Opt.Whisp2.addEventListener(Event.SELECT,this.changeNewWhispTabsForAll);
         Opt.Whisp3.label = this.locale.WHISP_CHAT_SETTING3;
         Opt.Whisp3.selected = this.newWhispTabsForFriend;
         Opt.Whisp3.addEventListener(Event.SELECT,this.changeNewWhispTabsForFriend);
         Opt.TitleBack.text = this.locale.TRANSPARENCY_SETTINGS_TITLE;
         Opt.BackChat.text = this.locale.TRANSPARENCY_SETTING_IN_CHAT;
         Opt.BackGame.text = this.locale.TRANSPARENCY_SETTING_IN_GAME;
         Opt.TitleVisual.text = this.locale.CHAT_APPEARANCE_SETTINGS_TITLE;
         Opt.Visual1.label = this.locale.CHAT_APPEARANCE_SETTING1;
         Opt.Visual1.selected = this.ShowTime;
         Opt.Visual1.addEventListener(Event.SELECT,this.changeShowTime);
         Opt.Visual2.label = this.locale.CHAT_APPEARANCE_SETTING2;
         Opt.Visual2.selected = this.ShowChan;
         Opt.Visual2.addEventListener(Event.SELECT,this.changeShowChan);
         Opt.Visual3.label = this.locale.CHAT_APPEARANCE_SETTING3;
         Opt.Visual3.selected = this.Scrolling;
         Opt.Visual3.addEventListener(Event.SELECT,this.changeScrolling);
         Opt.Visual4.label = this.locale.CHAT_APPEARANCE_SETTING4;
         Opt.Visual4.selected = this.HiddenMode;
         Opt.Visual4.enabled = true;
         Opt.Visual4.addEventListener(Event.SELECT,this.changeHiddenMode);
         Opt.Visual5.label = this.locale.CHAT_APPEARANCE_SETTING5;
         Opt.Visual5.selected = this.MissClickMode;
         Opt.Visual5.enabled = true;
         Opt.Visual5.addEventListener(Event.SELECT,this.changeMissClickMode);
         Opt.TitleChan.text = this.locale.CHANNELS_TITLE;
         Opt.TabName.addEventListener(Event.CHANGE,this.saveTitle);
         Opt.newForm.visible = false;
         Opt.OnlyEng.label = this.locale.ONLY_ENG;
         if(this.TabBar.selectedIndex < 0)
         {
            Opt.OnlyEng.selected = this.onlyEng;
            Opt.OnlyEng.addEventListener(Event.SELECT,this.changeOnlyEng);
            Opt.type = "";
            Opt.TabName.text = this._title;
            Opt.Chan.Channels.dataProvider = new DataProvider(this.settings);
            Opt.editBtn.visible = false;
            Opt.LoadBtn.visible = true;
            Opt.LoadBtn.label = this.locale.LOAD_SETTING;
            Opt.LoadBtn.addEventListener(MouseEvent.CLICK,this.loadSettings);
         }
         else
         {
            Opt.OnlyEng.selected = this.TabBar.Tabs[this.TabBar.selectedIndex].onlyEng;
            Opt.OnlyEng.addEventListener(Event.SELECT,this.changeOnlyEngTab);
            Opt.type = "setting";
            Opt.LoadBtn.label = this.locale.DEL_BTN_TITLE;
            this.TabBar.Tabs[this.TabBar.selectedIndex].label;
            Opt.TabName.text = this.TabBar.Tabs[this.TabBar.selectedIndex].label;
            Opt.Chan.Channels.dataProvider = new DataProvider(this.TabBar.Tabs[this.TabBar.selectedIndex].setting);
         }
         Opt.Func = this.EnterDown;
         addChild(Opt);
         Opt.Colors.addEventListener("COLORS_SHOW",this.sayShowColor);
         Opt.Colors.addEventListener("COLORS_HIDE",this.sayHideColor);
         Opt.Colors.Draw(Object(root).colors);
      }
      
      protected function focusOnInpt(e:MouseEvent) : *
      {
      }
      
      protected function changeOnlyEng(e:Event) : *
      {
         this.onlyEng = e.target.selected;
      }
      
      protected function changeOnlyEngTab(e:Event) : *
      {
         this.TabBar.Tabs[this.TabBar.selectedIndex].onlyEng = e.target.selected;
      }
      
      protected function changeNewWhispTabs(e:Event) : *
      {
         this.newWhispTabs = e.target.selected;
      }
      
      protected function changeNewWhispTabsForAll(e:Event) : *
      {
         this.newWhispTabsForAll = e.target.selected;
      }
      
      protected function changeNewWhispTabsForFriend(e:Event) : *
      {
         this.newWhispTabsForFriend = e.target.selected;
      }
      
      protected function changeShowTime(e:Event) : *
      {
         this.ShowTime = e.target.selected;
      }
      
      protected function changeShowChan(e:Event) : *
      {
         this.ShowChan = e.target.selected;
      }
      
      protected function changeScrolling(e:Event) : *
      {
         this.Scrolling = e.target.selected;
      }
      
      protected function changeHiddenMode(e:Event) : *
      {
         this.HiddenMode = e.target.selected;
      }
      
      protected function changeMissClickMode(e:Event) : *
      {
         this.MissClickMode = e.target.selected;
      }
      
      protected function changeChanalSettings(e:ChannelEvent) : *
      {
         if(this.TabBar.selectedIndex > -1)
         {
            this.TabBar.Tabs[this.TabBar.selectedIndex].setting[e.index].selected = e.selected;
            this.changeSetDropDown(this.TabBar.selectedIndex);
         }
         else
         {
            this.settings[e.index].selected = e.selected;
            this.changeSetDropDown(-1);
         }
      }
      
      protected function changeSoundSettings(e:ChannelEvent) : *
      {
         ChatSettings.setCh(e.id,e.sound);
         this.API.updateSoundSettings(ChatSettings.getSoundschannels());
      }
      
      protected function changeChanalColor(e:Event) : *
      {
         if(this.TabBar.selectedIndex > -1)
         {
            this.changeSetDropDown(this.TabBar.selectedIndex);
         }
         else
         {
            this.changeSetDropDown(-1);
         }
      }
      
      public function validateColors() : *
      {
         this.changeChanalColor(null);
         if(this.settingsOpenFlag && !this.colorsOpenFlag)
         {
            this.closeOptions();
            this.openOptions();
         }
      }
      
      public function closeOptions() : *
      {
         for(var i:* = 0; i < numChildren; i++)
         {
            if(getChildAt(i).name == "Settings")
            {
               addEventListener(MouseEvent.CLICK,this.focusOnInpt);
               removeChild(getChildAt(i));
               stage.focus = this.inpt;
               this.settingsOpenFlag = false;
            }
         }
      }
      
      protected function saveNewTab(e:MouseEvent, Option:Object) : *
      {
      }
      
      protected function saveOptions(O:Object) : *
      {
         if(this.TabBar.selectedIndex < 0)
         {
            this.settings = O.Chan.Channels.getChannels();
         }
         else
         {
            this.TabBar.Tabs[this.TabBar.selectedIndex].setting = O.Chan.Channels.getChanals();
         }
      }
      
      internal function clickOptions(e:MouseEvent) : *
      {
         if(!this.settingsOpenFlag)
         {
            this.openOptions();
         }
         else
         {
            this.closeOptions();
         }
      }
      
      protected function sliderChatChange(e:SliderEvent) : *
      {
         this._alphaChat = e.target.value;
         this.changeAlpha = e.target.value;
      }
      
      protected function sliderGameChange(e:SliderEvent) : *
      {
         this._alphaGame = e.target.value;
      }
      
      protected function sliderFontChange(e:SliderEvent) : *
      {
         this.fontSize = e.target.value;
         this.fontSizeChange(e.target.value);
      }
      
      public function fontSizeChange(N:Number) : *
      {
         this.defaultSize();
         if(this.inpt.textHeight + 6.6 > 20)
         {
            this.inpt.height = this.inpt.textHeight + 6.6;
         }
         this.inpt.y -= this.inpt.height - this.intpHeight;
         this.tf1.height = this.inpt.height;
         this.tf1.y = this.inpt.y;
         this.intpHeight = this.inpt.height;
         this.txt.height = this.hit.height - this.inpt.height - 10;
         this.sb.height = this.txt.height;
         this.defaultY = this.inpt.y;
         this.defaultTxtHe = this.txt.height;
         this.tf1.height = this.inpt.height;
         this.tf1.y = this.inpt.y;
      }
      
      public function setMode(mode:String) : *
      {
         this.closeOptions();
         if(getChildByName("ContextMenu") != null)
         {
            removeChild(getChildByName("ContextMenu"));
         }
         this.inpt.removeEventListener(KeyboardEvent.KEY_DOWN,this.CtrlDown);
         this.hiddenTxt.visible = false;
         this.CommandsList.dataProvider = new DataProvider();
         this.CommandsList.height = this.CommandsList.dataProvider.length * 22;
         this.inpt.removeEventListener(KeyboardEvent.KEY_DOWN,this.ArrowKeysDown);
         switch(mode)
         {
            case "game":
               this.setModeGame();
               break;
            case "chat":
               this.setModeChat();
               break;
            case "hidden":
               this.setModeHidden();
               break;
            case "half_hidden":
               this.setModeHalfHidden();
         }
      }
      
      protected function visibleModeShowen(event:Event) : *
      {
         this._animation = true;
         if(this.x < this.X || this.y > this.Y)
         {
            if(this.x < this.X)
            {
               this.x += this.X / 3;
            }
            if(this.y > this.Y)
            {
               this.y -= ((Object(root).height + stage.stageHeight) / 2 - this.Y) / 3;
            }
         }
         else
         {
            this._animation = false;
            this.x = this.X;
            this.y = this.Y;
            this.removeEventListener(Event.ENTER_FRAME,this.visibleModeShowen);
            this.validatePosition();
         }
      }
      
      protected function visibleModeHidden(event:Event) : *
      {
         this._animation = true;
         if(this.x > (Object(root).width - stage.stageWidth) / 2 || this.y < (Object(root).height + stage.stageHeight) / 2)
         {
            if(this.x > (Object(root).width - stage.stageWidth) / 2)
            {
               this.x -= this.X / 3;
            }
            if(this.y < (Object(root).height + stage.stageHeight) / 2)
            {
               this.y += ((Object(root).height + stage.stageHeight) / 2 - this.Y) / 3;
            }
         }
         else
         {
            this._animation = false;
            this.visible = false;
            this.x = (Object(root).width - stage.stageWidth) / 2;
            this.y = (Object(root).height + stage.stageHeight) / 2;
            this.removeEventListener(Event.ENTER_FRAME,this.visibleModeHidden);
         }
      }
      
      protected function visibleModeGame(event:Event) : *
      {
         if(this.alphaChat > this.alphaGame)
         {
            this.changeAlpha -= 0.05;
            if(this.changeAlpha <= this.alphaGame)
            {
               this.removeEventListener(Event.ENTER_FRAME,this.visibleModeGame);
            }
         }
         else
         {
            this.changeAlpha += 0.05;
            if(this.changeAlpha >= this.alphaGame)
            {
               this.removeEventListener(Event.ENTER_FRAME,this.visibleModeGame);
            }
         }
      }
      
      protected function visibleModeChat(event:Event) : *
      {
         if(this.alphaChat < this.alphaGame)
         {
            if(this.changeAlpha <= this.alphaChat)
            {
               this.removeEventListener(Event.ENTER_FRAME,this.visibleModeChat);
            }
            else
            {
               this.changeAlpha -= 0.05;
            }
         }
         else if(this.changeAlpha >= this.alphaChat)
         {
            this.removeEventListener(Event.ENTER_FRAME,this.visibleModeChat);
         }
         else
         {
            this.changeAlpha += 0.05;
         }
      }
      
      protected function visibleModeHalfHidden(event:Event) : *
      {
         if(this.changeAlpha <= 0)
         {
            this.removeEventListener(Event.ENTER_FRAME,this.visibleModeHalfHidden);
            this.changeVisible(false);
         }
         else
         {
            this.changeAlpha -= 0.05;
         }
      }
      
      public function setModeHidden() : *
      {
         removeEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         this.closeOptions();
         this.tf1.close();
         if(this.x > (Object(root).width - stage.stageWidth) / 2 && this.y < (Object(root).height + stage.stageHeight) / 2 - 20 && !this._animation)
         {
            this.X = this.x;
            this.Y = this.y;
         }
         this.x = this.X;
         this.y = this.Y;
         this.removeEventListener(Event.ENTER_FRAME,this.visibleModeShowen);
         this._animation = true;
         this.addEventListener(Event.ENTER_FRAME,this.visibleModeHidden);
      }
      
      public function setModeGame() : *
      {
         this.removeEventListener(Event.ENTER_FRAME,this.visibleModeHalfHidden);
         removeEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         this.closeOptions();
         this.tf1.close();
         this.X = this.x;
         this.Y = this.Y;
         this.inpt.visible = false;
         this.tf1.visible = false;
         this.minBtn.visible = false;
         this.sb.visible = false;
         this.ramka.visible = false;
         this.titleBtn.visible = true;
         this.OptBtn.visible = false;
         this.sb.visible = false;
         this.txt.visible = true;
         this.hiddenTxt.visible = false;
         this.translateBtn.visible = false;
         this.addEventListener(Event.ENTER_FRAME,this.visibleModeGame);
         this.changeAlpha = this.alphaChat;
         this.updateRadioMode("setModeGame");
      }
      
      public function setModeHalfHidden() : *
      {
         this.removeEventListener(Event.ENTER_FRAME,this.visibleModeHalfHidden);
         removeEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         this.closeOptions();
         this.tf1.close();
         this.X = this.x;
         this.Y = this.Y;
         this.inpt.visible = false;
         this.tf1.visible = false;
         this.minBtn.visible = false;
         this.ramka.visible = false;
         this.OptBtn.visible = false;
         this.sb.visible = false;
         this.txt.visible = false;
         this.hiddenTxt.visible = false;
         this.translateBtn.visible = false;
         this.addEventListener(Event.ENTER_FRAME,this.visibleModeHalfHidden);
         this.changeAlpha = this.alphaChat;
         this.updateRadioMode("setModeHalfHidden");
      }
      
      public function setModeChat() : *
      {
         this.inpt.addEventListener(KeyboardEvent.KEY_DOWN,this.CtrlDown);
         addEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         this.removeEventListener(Event.ENTER_FRAME,this.visibleModeHalfHidden);
         this.closeOptions();
         if(!(!Object(root).hiddenBtn.visible && !this.visible))
         {
            this.removeEventListener(Event.ENTER_FRAME,this.visibleModeHidden);
            this.x = this.X;
            this.y = this.Y;
            this.validatePosition();
         }
         if(!this.visible)
         {
            this.x = (Object(root).width - stage.stageWidth) / 2;
            this.y = (Object(root).height + stage.stageHeight) / 2;
            this.visible = true;
            this._animation = true;
            this.addEventListener(Event.ENTER_FRAME,this.visibleModeShowen);
         }
         this.changeVisible(true);
         if(this.tf1.dataArray.length != 0)
         {
            this.inpt.visible = true;
            this.tf1.visible = true;
         }
         this.hiddenTxt.visible = false;
         this.addEventListener(Event.ENTER_FRAME,this.visibleModeChat);
         this.changeAlpha = this.alphaGame;
         this.updateRadioMode("setModeChat");
      }
      
      internal function addUser(e:Event) : void
      {
         Object(root).ChatFocus = this.inpt;
         this.inpt.textColor = this.tf1.color;
         if(this.tf1.curID == -1)
         {
            this.tf1.selectedIndex = 0;
         }
         this.API.setIMEmode(true);
      }
      
      protected function changeVisible(vis:Boolean) : *
      {
         this.OptBtn.visible = vis;
         this.background.visible = vis;
         this.titleBtn.visible = vis;
         this.txt.visible = vis;
         if(this.inpt.type == TextFieldType.INPUT || !vis)
         {
            this.inpt.visible = vis;
            this.tf1.visible = vis;
         }
         this.ramka.visible = vis;
         this.OptBtn.visible = vis;
         this.sb.visible = vis;
         this.newBtn.visible = vis;
         this.TabBar.visible = vis;
         this.minBtn.visible = vis;
      }
      
      public function set changeAlpha(value:Number) : void
      {
         this.background.alpha = value;
         this.titleBtn.alpha = value;
         this.titleBtn.textField.alpha = 1;
         this.newBtn.alpha = value;
         this.TabBar.alpha = value;
         this.OptBtn.background.alpha = value;
         this.txt.alpha = 1;
      }
      
      public function get changeAlpha() : Number
      {
         return this.background.alpha;
      }
      
      internal function delUser(e:Event) : void
      {
         this.API.setIMEmode(false);
      }
      
      internal function clearMsg() : void
      {
         this.haveMsg = false;
         this.inpt.text = "";
      }
      
      internal function AllMsgEv(e:ButtonEvent) : *
      {
         this.AllMsg();
         stage.focus = this.inpt;
         this.changeTab(null);
      }
      
      public function AllMsg() : *
      {
         this.changeSetDropDown(-1);
         this.titleBtn.STATE = "over";
         this.titleBtn.setState("over");
         this.TabBar.selectedIndex = -1;
         this.inpt.textColor = this.tf1.color;
         this.txt.htmlText = this.mainMsgs;
         this.TabBar.TabBarFirst.invalidateSize();
         this.TabBar.TabBarSec.invalidateSize();
         if(this.settingsOpenFlag)
         {
            this.closeOptions();
            this.openOptions();
         }
         this.fontSize = this._fontSize;
      }
      
      internal function MainMsg(e:MouseEvent) : *
      {
      }
      
      internal function focusOUT(e:FocusEvent) : *
      {
         Object(root).setChildIndex(this,0);
      }
      
      internal function focusIN(e:FocusEvent) : *
      {
         if(this != Object(root).getChildAt(Object(root).numChildren - 2))
         {
            Object(root).swapChildren(this,Object(root).getChildAt(Object(root).numChildren - 2));
         }
      }
      
      override protected function draw() : void
      {
         if(isInvalid("source"))
         {
            this.loadSource();
            this.reflowContent();
         }
         else if(isInvalid("padding"))
         {
            this.reflowContent();
         }
         if(isInvalid(InvalidationType.SIZE))
         {
            constraints.update(_width,_height);
         }
         this.intpHeight = this.inpt.height;
         this.defaultY = this.inpt.y;
         this.defaultTxtHe = this.txt.height;
         this.fontSize = this.fontSize;
      }
      
      protected function loadSource() : void
      {
         var classRef:Class = null;
         if(this._src != "")
         {
            if(this._content)
            {
               constraints.removeElement("content");
               removeChild(this._content);
            }
            classRef = getDefinitionByName(this._src) as Class;
            if(!classRef)
            {
               this._content = null;
               return;
            }
            this._content = new classRef();
            addChild(this._content);
            constraints.addElement("content",this._content,Constraints.ALL);
            this._content.name = "content";
         }
      }
      
      protected function reflowContent() : void
      {
         if(!this._content)
         {
            return;
         }
         var p:Padding = this._contentPadding;
         var element:ConstrainedElement = constraints.getElement("content");
         this._content.x = element.left = p.left;
         this._content.y = element.top = p.top;
         element.right = p.right;
         element.bottom = p.bottom;
         this._content.width = _width - p.horizontal;
         this._content.height = _height - p.vertical;
         invalidateSize();
      }
      
      protected function onWindowStartDrag(e:Event) : void
      {
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false,0,true);
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onWindowsMoveDrag,false,0,true);
         stage.addEventListener(MouseEvent.MIDDLE_MOUSE_UP,this.onWindowStopDrag,false,0,true);
         this.titleBtn.removeEventListener(ButtonEvent.CLICK,this.AllMsgEv);
         startDrag();
      }
      
      protected function onWindowsMoveDrag(e:Event) : void
      {
         this.closeOptions();
      }
      
      public function validatePosition() : *
      {
         if(y < (Object(root).height - stage.stageHeight) / 2)
         {
            y = (Object(root).height - stage.stageHeight) / 2;
         }
         if(x < (Object(root).width - stage.stageWidth) / 2)
         {
            x = (Object(root).width - stage.stageWidth) / 2;
         }
         if(y > (Object(root).height + stage.stageHeight) / 2 - 40)
         {
            y = (Object(root).height + stage.stageHeight) / 2 - 70;
         }
         if(x > (Object(root).width + stage.stageWidth) / 2 - 101)
         {
            x = (Object(root).width + stage.stageWidth) / 2 - 101;
         }
         this.X = this.x;
         this.Y = this.y;
      }
      
      protected function onWindowStopDrag(e:Event) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false);
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onWindowsMoveDrag);
         stage.removeEventListener(MouseEvent.MIDDLE_MOUSE_UP,this.onWindowStopDrag);
         stopDrag();
         this.titleBtn.addEventListener(ButtonEvent.CLICK,this.AllMsgEv,false,0,true);
         this.validatePosition();
      }
      
      protected function onResizeStartDrag(e:Event) : void
      {
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onResizeStopDrag,false,0,true);
         this.defaultSize();
         this.closeOptions();
         this._dragProps = [parent.mouseX - (x + width),parent.mouseY - (y + height),parent.mouseY];
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onResizeMouseMove,false,0,true);
      }
      
      protected function onResizeStopDrag(e:Event) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onResizeMouseMove,false);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onResizeStopDrag,false);
         this.intpHeight = this.inpt.height;
         this.defaultY = this.inpt.y;
         this.defaultTxtHe = this.txt.height;
      }
      
      protected function onResizeMouseMove(e:Event) : *
      {
         this.TabBar.resizeTab();
         this.tf1.y = this.inpt.y;
         var w:Number = Math.max(this.minWidth,Math.min(this.maxWidth,parent.mouseX - x - this._dragProps[0]));
         var h:Number = Math.max(this.minHeight,Math.min(this.maxHeight,parent.mouseY - y - this._dragProps[1]));
         if(w != _width || h != _height)
         {
            setSize(w,h);
            dispatchEvent(new ResizeEvent(ResizeEvent.RESIZE,scaleX,scaleY));
         }
         this.ScrollMaxTxt();
      }
      
      protected function onResizeStartDragUP(e:Event) : void
      {
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onResizeStopDragUP,false,0,true);
         this.defaultSize();
         this.closeOptions();
         this._dragPropsUP = [parent.mouseX - (x + width),parent.mouseY - (y + height),parent.mouseY];
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onResizeMouseMoveUP,false,0,true);
      }
      
      protected function onResizeStopDragUP(e:Event) : void
      {
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onResizeMouseMoveUP,false);
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onResizeStopDragUP,false);
         this.intpHeight = this.inpt.height;
         this.defaultY = this.inpt.y;
         this.defaultTxtHe = this.txt.height;
         this.X = x;
         this.Y = y;
      }
      
      protected function onResizeMouseMoveUP(e:Event) : *
      {
      }
      
      internal function sub_string_len(text:String, len:Number = 43000) : *
      {
         var index:Number = NaN;
         var new_text:String = null;
         var end_line_format:String = "</TEXTFORMAT>";
         index = Math.max(text.length - len,0);
         if(index > 0)
         {
            new_text = text.substr(index);
            index = Number(new_text.indexOf(end_line_format));
            if(index != -1)
            {
               index += end_line_format.length;
               if(index < new_text.length)
               {
                  new_text = new_text.substr(index);
               }
            }
         }
         else
         {
            new_text = text;
         }
         return new_text;
      }
      
      public function drawMsg(type:Number, time:String, msg:String, usr:String = "", clan:String = "", returned:Boolean = false, onlyEn:Boolean = false) : Boolean
      {
         var i:* = undefined;
         var Obj1:Object = null;
         var Flag:Boolean = false;
         var Obj:Object = new Object();
         Obj = this.parse(type,this.settings);
         var str:* = "";
         if(this.txt.htmlText.length > this.MSG_HTML_LIMIT_FULL)
         {
            this.txt.htmlText = this.sub_string_len(this.txt.htmlText);
         }
         if(Obj.selected)
         {
            if(Obj.com != null)
            {
               if(onlyEn && this.onlyEng || !this.onlyEng || returned)
               {
                  str = "";
                  str = "<font size=\"" + this.fontSize + "\" color=\"#" + ChannelColors.getAt(this.settings[Obj.i].id).toString(16) + "\">";
                  if(this.ShowTime)
                  {
                     str = str + "[" + time + "]";
                  }
                  if(this.ShowChan)
                  {
                     str = str + "[" + this.settings[Obj.i].label + "]";
                  }
                  if(clan != "")
                  {
                     str = str + "[" + clan + "]";
                  }
                  if(returned)
                  {
                     if(type == this.whispID)
                     {
                        str = str + "<font color=\'" + this.USER_COLOR + "\'>[" + this.locale.YOU + "]</font>-";
                     }
                     else
                     {
                        usr = this.locale.YOU;
                     }
                  }
                  if(usr != "" && usr != null)
                  {
                     if(usr == this.locale.YOU)
                     {
                        str = str + "<font color=\'" + this.USER_COLOR + "\'>[" + usr + "]</font>";
                     }
                     else
                     {
                        str = str + "<a href=\"event:" + usr + "\"><font color=\'" + this.USER_COLOR + "\'>[" + usr + "]</font></a>";
                     }
                  }
                  str = str + ": " + msg + "</font>\n";
                  this.mainMsgs += str;
                  if(this.mainMsgs.length > this.MSG_HTML_LIMIT_FULL)
                  {
                     this.mainMsgs = this.sub_string_len(this.mainMsgs);
                  }
                  if(this.TabBar.selectedIndex == -1)
                  {
                     this.txt.appendText(str);
                     if(!this.txt.visible)
                     {
                        this.NotificationStart(str);
                     }
                     if(this.Scrolling)
                     {
                        this.ScrollMaxTxt();
                     }
                  }
               }
            }
            else
            {
               str = "";
               str = "<font size=\"" + this.fontSize + "\" color=\"#" + ChannelColors.getAt(this.settings[Obj.i].id).toString(16) + "\">";
               if(this.ShowTime)
               {
                  str = str + "[" + time + "]";
               }
               if(this.ShowChan)
               {
                  str = str + "[" + this.settings[Obj.i].label + "]";
               }
               if(clan != "")
               {
                  str = str + "[" + clan + "]";
               }
               str = str + ": " + msg + "</font>\n";
               this.mainMsgs += str;
               if(this.mainMsgs.length > this.MSG_HTML_LIMIT_FULL)
               {
                  this.mainMsgs = this.sub_string_len(this.mainMsgs);
               }
               if(this.TabBar.selectedIndex == -1)
               {
                  this.txt.appendText(str);
                  if(!this.txt.visible)
                  {
                     this.NotificationStart(str);
                  }
                  if(this.Scrolling)
                  {
                     this.ScrollMaxTxt();
                  }
               }
            }
         }
         for(i in this.TabBar.Tabs)
         {
            Obj1 = new Object();
            Obj1 = this.parse(type,this.TabBar.Tabs[i].setting);
            if(this.TabBar.Tabs[i].whisp && usr == this.TabBar.Tabs[i].user && type == 9)
            {
               str = "";
               str = "<font size=\"" + this.fontSize + "\" color=\"#" + ChannelColors.getAt(this.TabBar.Tabs[i].setting[0].id).toString(16) + "\">";
               if(this.ShowTime)
               {
                  str = str + "[" + time + "]";
               }
               if(clan != "")
               {
                  str = str + "[" + clan + "]";
               }
               if(returned)
               {
                  str = str + "<font color=\'" + this.USER_COLOR + "\'>[" + this.locale.YOU + "]</font>-";
               }
               if(usr != "" && usr != null)
               {
                  str = str + "<a href=\"event:" + usr + "\"><font color=\'" + this.USER_COLOR + "\'>[" + usr + "]</font></a>";
               }
               str = str + ": " + msg + "</font>\n";
               this.TabBar.Tabs[i].TEXT += str;
               if(this.TabBar.Tabs[i].TEXT.length > this.MSG_HTML_LIMIT_FULL)
               {
                  this.TabBar.Tabs[i].TEXT = this.sub_string_len(this.TabBar.Tabs[i].TEXT);
               }
               Flag = true;
               if(this.TabBar.selectedIndex == i)
               {
                  this.txt.appendText(str);
                  if(!this.txt.visible)
                  {
                     this.NotificationStart(str);
                  }
                  if(this.Scrolling)
                  {
                     this.ScrollMaxTxt();
                  }
               }
               else
               {
                  this.TabBar.setNotification(i,true);
                  this.TabBar.validateNotif();
               }
            }
            if(Obj1.selected)
            {
               if(Obj1.com != null)
               {
                  if(onlyEn && this.TabBar.Tabs[i].onlyEng || !this.TabBar.Tabs[i].onlyEn || returned)
                  {
                     str = "";
                     str = "<font size=\"" + this.fontSize + "\" color=\"#" + ChannelColors.getAt(this.TabBar.Tabs[i].setting[Obj1.i].id).toString(16) + "\">";
                     if(this.ShowTime)
                     {
                        str = str + "[" + time + "]";
                     }
                     if(this.ShowChan)
                     {
                        str = str + "[" + this.TabBar.Tabs[i].setting[Obj1.i].label + "]";
                     }
                     if(clan != "")
                     {
                        str = str + "[" + clan + "]";
                     }
                     if(returned)
                     {
                        if(type == this.whispID)
                        {
                           str = str + "<font color=\'" + this.USER_COLOR + "\'>[" + this.locale.YOU + "]</font>-";
                        }
                        else
                        {
                           usr = this.locale.YOU;
                        }
                     }
                     if(usr != "" && usr != null)
                     {
                        if(usr == this.locale.YOU)
                        {
                           str = str + "<font color=\'" + this.USER_COLOR + "\'>[" + usr + "]</font>";
                        }
                        else
                        {
                           str = str + "<a href=\"event:" + usr + "\"><font color=\'" + this.USER_COLOR + "\'>[" + usr + "]</font></a>";
                        }
                     }
                     str = str + ": " + msg + "</font>\n";
                     this.TabBar.Tabs[i].TEXT += str;
                     if(this.TabBar.Tabs[i].TEXT.length > this.MSG_HTML_LIMIT_FULL)
                     {
                        this.TabBar.Tabs[i].TEXT = this.sub_string_len(this.TabBar.Tabs[i].TEXT);
                     }
                     if(this.TabBar.selectedIndex == i)
                     {
                        this.txt.appendText(str);
                        if(!this.txt.visible)
                        {
                           this.NotificationStart(str);
                        }
                        if(this.Scrolling)
                        {
                           this.ScrollMaxTxt();
                        }
                     }
                  }
               }
               else
               {
                  str = "";
                  str = "<font size=\"" + this.fontSize + "\" color=\"#" + ChannelColors.getAt(this.TabBar.Tabs[i].setting[Obj1.i].id).toString(16) + "\">";
                  if(this.ShowTime)
                  {
                     str = str + "[" + time + "]";
                  }
                  if(this.ShowChan)
                  {
                     str = str + "[" + this.TabBar.Tabs[i].setting[Obj1.i].label + "]";
                  }
                  if(clan != "")
                  {
                     str = str + "[" + clan + "]";
                  }
                  str = str + ": " + msg + "</font>\n";
                  this.TabBar.Tabs[i].TEXT += str;
                  if(this.TabBar.Tabs[i].TEXT.length > this.MSG_HTML_LIMIT_FULL)
                  {
                     this.TabBar.Tabs[i].TEXT = this.sub_string_len(this.TabBar.Tabs[i].TEXT);
                  }
                  if(this.TabBar.selectedIndex == i)
                  {
                     this.txt.appendText(str);
                     if(!this.txt.visible)
                     {
                        this.NotificationStart(str);
                     }
                     if(this.Scrolling)
                     {
                        this.ScrollMaxTxt();
                     }
                  }
               }
            }
         }
         return Flag;
      }
      
      internal function parse(a:Number, arr:Array) : Object
      {
         var Ob:Object = new Object();
         Ob.i = -1;
         Ob.selected = false;
         for(var i:* = 0; i < arr.length; i++)
         {
            if(arr[i].id == a)
            {
               Ob.i = i;
               Ob.selected = arr[i].selected;
               Ob.com = arr[i].com;
               return Ob;
            }
         }
         return Ob;
      }
      
      public function DropSetting() : *
      {
         this.TabBar.Drop();
         this.mainMsgs = "";
         this.AllMsg();
         this.inpt.text = "";
      }
      
      protected function onCloseButtonClick(e:MouseEvent) : void
      {
         parent.removeChild(this);
         dispatchEvent(new ComponentEvent(ComponentEvent.HIDE));
      }
      
      public function SaveWindows() : Object
      {
         var setObj1:Object = null;
         var j:* = undefined;
         var tmpObj:Object = null;
         var setObj:Object = null;
         var s:String = null;
         var setObj2:Object = null;
         var Ob:Object = new Object();
         for(var i:* = 0; i < Object(root).numChildren; i++)
         {
            if(Object(root).getChildAt(i).name != "sb" && Object(root).getChildAt(i).name != "s1" && Object(root).getChildAt(i).name != "s2" && Object(root).getChildAt(i).name != "s3" && Object(root).getChildAt(i).name != "s4" && Object(root).getChildAt(i).name != "btn" && Object(root).getChildAt(i).name != "Log" && Object(root).getChildAt(i).name != "hiddenBtn" && Object(root).getChildAt(i).name != "Message" && Object(root).getChildAt(i).name != "imeComponent")
            {
               if(Object(root).getChildAt(i).name == "MainChat")
               {
                  Ob.MainWindow = new Object();
                  Ob["MainWindow"].tabs = new Object();
                  Ob["MainWindow"].x = (Object(root).MainChat.X - (Object(root).width - stage.stageWidth) / 2) / stage.stageWidth;
                  Ob["MainWindow"].y = Object(root).MainChat.Y / ((Object(root).height + stage.stageHeight) / 2);
                  Ob["MainWindow"].height = Object(root).MainChat.height;
                  Ob["MainWindow"].width = Object(root).MainChat.width;
                  setObj1 = this.Parse.unparse(Object(root).MainChat.settings);
                  Ob["MainWindow"].channals = setObj1.channels;
                  Ob["MainWindow"].transpGame = Object(root).MainChat.alphaGame;
                  Ob["MainWindow"].transpChat = Object(root).MainChat.alphaChat;
                  Ob["MainWindow"].time = Object(root).MainChat.ShowTime;
                  Ob["MainWindow"].scroll = Object(root).MainChat.Scrolling;
                  Ob["MainWindow"].chanal = Object(root).MainChat.ShowChan;
                  Ob["MainWindow"].forfriend = Object(root).MainChat.newWhispTabsForFriend;
                  Ob["MainWindow"].forall = Object(root).MainChat.newWhispTabsForAll;
                  Ob["MainWindow"].whispnewtab = Object(root).MainChat.newWhispTabs;
                  Ob["MainWindow"].onlyEng = Object(root).MainChat.onlyEng;
                  Ob["MainWindow"].HiddenMode = Object(root).MainChat.HiddenMode;
                  Ob["MainWindow"].MissClickMode = Object(root).MainChat.MissClickMode;
                  Ob["MainWindow"].fontSize = Object(root).MainChat.fontSize;
                  for(j = 0; j < Object(root).MainChat.TabBar.Tabs.length; j++)
                  {
                     tmpObj = new Object();
                     tmpObj.label = Object(root).MainChat.TabBar.Tabs[j].label;
                     setObj = this.Parse.unparse(Object(root).MainChat.TabBar.Tabs[j].setting);
                     tmpObj.channals = setObj.channels;
                     tmpObj.user = Object(root).MainChat.TabBar.Tabs[j].user;
                     tmpObj.whisp = Object(root).MainChat.TabBar.Tabs[j].whisp;
                     tmpObj.onlyEng = Object(root).MainChat.TabBar.Tabs[j].onlyEng;
                     tmpObj.defaultTab = Object(root).MainChat.TabBar.Tabs[j].defaultTab;
                     if(tmpObj.label == this.locale.ENGLISH)
                     {
                        Ob["MainWindow"].tabs[this.locale.ENGLISH] = tmpObj;
                     }
                     else
                     {
                        Ob["MainWindow"].tabs["Tab" + j] = tmpObj;
                     }
                  }
               }
               else
               {
                  s = "Window" + i;
                  Ob[s] = new Object();
                  Ob[s].label = Object(root).getChildAt(i).title;
                  Ob[s].x = (Object(root).getChildAt(i).X - (Object(root).width - stage.stageWidth) / 2) / stage.stageWidth;
                  Ob[s].y = Object(root).getChildAt(i).Y / ((Object(root).height + stage.stageHeight) / 2);
                  Ob[s].height = Object(root).getChildAt(i).height;
                  Ob[s].width = Object(root).getChildAt(i).width;
                  setObj2 = this.Parse.unparse(Object(root).getChildAt(i).settings);
                  Ob[s].channals = setObj2.channels;
                  Ob[s].transpGame = Object(root).getChildAt(i).alphaGame;
                  Ob[s].transpChat = Object(root).getChildAt(i).alphaChat;
                  Ob[s].time = Object(root).getChildAt(i).ShowTime;
                  Ob[s].scroll = Object(root).getChildAt(i).Scrolling;
                  Ob[s].chanal = Object(root).getChildAt(i).ShowChan;
                  Ob[s].user = Object(root).getChildAt(i).user;
                  Ob[s].whisp = Object(root).getChildAt(i).whisp;
                  Ob[s].onlyEng = Object(root).getChildAt(i).onlyEng;
                  Ob[s].defaultTab = Object(root).getChildAt(i).defaultTab;
                  Ob[s].fontSize = Object(root).getChildAt(i).fontSized;
               }
            }
         }
         Ob["colors"] = ChannelColors.getAllColors();
         return Ob;
      }
      
      public function saveSettings(e:Event) : *
      {
         var f1:Function;
         var f2:Function;
         var MSG:Message = null;
         MSG = new Message();
         MSG.name = "Message";
         f1 = function():*
         {
            Object(root).removeChild(MSG);
            API.modalMode(false);
            API.saveSettings(SaveWindows());
            stage.focus = inpt;
         };
         f2 = function():*
         {
            Object(root).removeChild(MSG);
            API.modalMode(false);
            stage.focus = inpt;
         };
         Object(root).addChild(MSG);
         stage.focus = MSG;
         if(MSG != Object(root).getChildAt(Object(root).numChildren - 2))
         {
            Object(root).swapChildren(MSG,Object(root).getChildAt(Object(root).numChildren - 2));
         }
         this.API.modalMode(true);
         MSG.Message1(this.locale.WARNING,this.locale.MSG_SAVE,f1,f2,this.locale.YES,this.locale.NO);
      }
      
      public function isRadioChannel(chan_id:int) : Boolean
      {
         var i:* = undefined;
         var find:Boolean = false;
         for(i in Object(root).MainChat.defaultChannals)
         {
            if(Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.WHISPER || Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.CLAN || Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.GROUP)
            {
               find ||= chan_id == Object(root).MainChat.defaultChannals[i].id;
            }
         }
         return find;
      }
      
      public function findWhispID() : Number
      {
         var i:* = undefined;
         for(i in Object(root).MainChat.defaultChannals)
         {
            if(Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.WHISPER)
            {
               return Object(root).MainChat.defaultChannals[i].id;
            }
         }
         return -1;
      }
      
      public function findNewsID() : Number
      {
         var i:* = undefined;
         for(i in Object(root).MainChat.defaultChannals)
         {
            if(Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.NEWS)
            {
               return Object(root).MainChat.defaultChannals[i].id;
            }
         }
         return -1;
      }
      
      public function findClanID() : Number
      {
         var i:* = undefined;
         for(i in Object(root).MainChat.defaultChannals)
         {
            if(Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.CLAN)
            {
               return Object(root).MainChat.defaultChannals[i].id;
            }
         }
         return -1;
      }
      
      public function findSysID() : Number
      {
         var i:* = undefined;
         for(i in Object(root).MainChat.defaultChannals)
         {
            if(Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.SYSTEM)
            {
               return Object(root).MainChat.defaultChannals[i].id;
            }
         }
         return -1;
      }
      
      public function loadSettings(e:Event) : *
      {
         var f1:Function;
         var f2:Function;
         var MSG:Message = null;
         MSG = new Message();
         MSG.name = "Message";
         f1 = function():*
         {
            Object(root).removeChild(MSG);
            API.modalMode(false);
            API.defaultSettings();
            stage.focus = inpt;
         };
         f2 = function():*
         {
            Object(root).removeChild(MSG);
            API.modalMode(false);
            stage.focus = inpt;
         };
         Object(root).addChild(MSG);
         stage.focus = MSG;
         if(MSG != Object(root).getChildAt(Object(root).numChildren - 2))
         {
            Object(root).swapChildren(MSG,Object(root).getChildAt(Object(root).numChildren - 2));
         }
         this.API.modalMode(true);
         MSG.Message1(this.locale.WARNING,this.locale.MSG_LOAD,f1,f2,this.locale.YES,this.locale.NO);
      }
      
      protected function NotificationStart(str:String) : *
      {
         this.hiddenTxt.appendText(str);
         this.hiddenTxt.scrollV = this.hiddenTxt.numLines;
         this.hiddenTxt.visible = true;
         this.hiddenTxt.removeEventListener(Event.ENTER_FRAME,this.AnimationNorif);
         this.hiddenTxt.removeEventListener(Event.ENTER_FRAME,this.AnimationNorifHide);
         this.hiddenTxt.addEventListener(Event.ENTER_FRAME,this.AnimationNorif);
      }
      
      protected function AnimationNorifHide(e:Event) : *
      {
         if(this.hiddenTxt.alpha > 0)
         {
            this.hiddenTxt.alpha -= 0.02;
         }
         else
         {
            this.hiddenTxt.removeEventListener(Event.ENTER_FRAME,this.AnimationNorif);
            this.hiddenTxt.removeEventListener(Event.ENTER_FRAME,this.AnimationNorifHide);
            this.hiddenTxt.visible = false;
            this.hiddenTxt.htmlText = "";
         }
      }
      
      protected function AnimationNorif(e:Event) : *
      {
         var Fun:* = undefined;
         if(this.hiddenTxt.alpha < 1)
         {
            this.hiddenTxt.alpha += 0.2;
         }
         else
         {
            this.hiddenTxt.removeEventListener(Event.ENTER_FRAME,this.AnimationNorif);
            Fun = function():*
            {
               hiddenTxt.addEventListener(Event.ENTER_FRAME,AnimationNorifHide);
            };
            setTimeout(Fun,Object(root).Delay);
         }
      }
      
      public function clearChat() : *
      {
         var i:* = undefined;
         for(i in this.TabBar.Tabs)
         {
            this.TabBar.Tabs[i].TEXT = "";
         }
         this.mainMsgs = "";
         this.txt.htmlText = "";
      }
   }
}


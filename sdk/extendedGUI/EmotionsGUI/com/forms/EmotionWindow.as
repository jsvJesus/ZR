package com.forms
{
   import com.ResizeFrameEvent;
   import com.communication.GameCommunication;
   import com.events.EmotionEvent;
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import flash.external.ExternalInterface;
   import flash.utils.setTimeout;
   import scaleform.clik.constants.ConstrainMode;
   import scaleform.clik.constants.InvalidationType;
   import scaleform.clik.controls.Button;
   import scaleform.clik.core.UIComponent;
   import scaleform.clik.utils.ConstrainedElement;
   import scaleform.clik.utils.Constraints;
   import scaleform.clik.utils.Padding;
   
   public class EmotionWindow extends UIComponent
   {
      public var frame_thumb:Frame_Thumb;
      
      public var frame_thumb_down:Frame_Thumb;
      
      public var frame_thumb_right:Frame_Thumb;
      
      public var frame_thumb_up:Frame_Thumb;
      
      public var locale:Object = new Object();
      
      public var minWidth:Number = 150;
      
      public var maxWidth:Number = 230;
      
      public var minHeight:Number = 200;
      
      public var maxHeight:Number = 2000;
      
      public var intpHeight:Number = 0;
      
      protected var _title:String;
      
      protected var _src:String = "";
      
      protected var _contentPadding:Padding;
      
      protected var _content:DisplayObject;
      
      protected var _dragProps:Array;
      
      protected var _dragPropsUP:Array;
      
      protected var _options:Boolean;
      
      protected var _current_sel_num:Number = 0;
      
      protected var X:Number = x;
      
      protected var Y:Number = y;
      
      public var closeBtn:Button;
      
      public var titleBtn:Button;
      
      public var background:MovieClip;
      
      public var background_title:MovieClip;
      
      public var hit:MovieClip;
      
      public var list:EmotionList;
      
      public var current_list:EmotionList;
      
      public var sb:DefaultScrollBar;
      
      public var sb_current:DefaultScrollBar;
      
      public var ramka:ResizeFrame;
      
      public var optionButton:Button;
      
      protected var localization:Object;
      
      public function EmotionWindow()
      {
         super();
         this._contentPadding = new Padding(0,0,0,0);
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
      
      public function get source() : String
      {
         return this._src;
      }
      
      public function set source(value:String) : void
      {
         this._src = value;
         invalidate("source");
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
      }
      
      override protected function configUI() : void
      {
         initSize();
         if(hitArea != null)
         {
            constraints.addElement("hitArea",hitArea,Constraints.ALL);
            constraints.addElement("sb",this.sb,Constraints.TOP | Constraints.BOTTOM | Constraints.LEFT);
            constraints.addElement("sb_current",this.sb_current,Constraints.TOP | Constraints.BOTTOM | Constraints.LEFT);
         }
         if(this.background != null)
         {
            constraints.addElement("background",this.background,Constraints.ALL);
         }
         if(this.background_title != null)
         {
            constraints.addElement("background_title",this.background_title,Constraints.TOP | Constraints.LEFT | Constraints.RIGHT);
         }
         if(this.titleBtn != null)
         {
            this.titleBtn.label = this._title || "Жесты11111";
            this.titleBtn.addEventListener(MouseEvent.MOUSE_DOWN,this.onWindowStartDrag,false,0,true);
            constraints.addElement("titleBtn",this.titleBtn,Constraints.TOP | Constraints.LEFT | Constraints.RIGHT);
         }
         if(this.closeBtn != null)
         {
            this.closeBtn.addEventListener(MouseEvent.CLICK,this.onCloseButtonClick,false,0,true);
            constraints.addElement("closeBtn",this.closeBtn,Constraints.TOP | Constraints.RIGHT);
         }
         if(this.optionButton != null)
         {
            constraints.addElement("optionButton",this.optionButton,Constraints.TOP | Constraints.LEFT);
            this.optionButton.addEventListener(MouseEvent.CLICK,this.onOptionClick);
         }
         if(this.list != null)
         {
            constraints.addElement("list",this.list,Constraints.ALL);
            constraints.addElement("current_list",this.current_list,Constraints.ALL);
         }
         addEventListener(MouseEvent.MIDDLE_MOUSE_DOWN,this.onWindowStartDrag,false,0,true);
         this.list.addEventListener(EmotionEvent.CHECK,this.onEmotionCheck);
         this.list.addEventListener(EmotionEvent.KEY_BIND,this.onEmotionKeyChange);
         this.current_list.addEventListener(EmotionEvent.CLICK,this.onEmotionClick);
         this.list.visible = false;
         this.sb.visible = false;
         this.ramka.addEventListener(ResizeFrameEvent.RESIZE,this.onResizeFrame);
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
         this.ramka.height = this.height + 12;
         this.ramka.width = this.width + 12;
         ExternalInterface.addCallback("set_animations",this.setAllAnimations);
         ExternalInterface.addCallback("set_current_animations",this.setAnimations);
         ExternalInterface.addCallback("reset_position",this.resetPosition);
         ExternalInterface.addCallback("doReposition",this.validatePosition);
         ExternalInterface.addCallback("get_settings",this.openSettings);
         ExternalInterface.addCallback("set_local",this.setLocal);
         ExternalInterface.addCallback("load_position",this.load_position);
         setTimeout(GameCommunication.ready,0);
      }
      
      protected function resetPosition(Obj:*) : *
      {
         this.x = (Object(root).width - stage.stageWidth) / 2 + 15.5;
         this.y = 360.75;
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
         this.width = W;
         this.height = H;
      }
      
      protected function setLocal(obj:Object) : *
      {
         this.localization = obj;
         this.titleBtn.label = this.localization.Title || "None";
         Object(root).keyBindText.text = this.localization.BIND_KEY || "None";
         Object(root).toolTip.toolTipText.text = this.localization.Help || "None";
      }
      
      protected function setAllAnimations(obj:Object) : *
      {
         Object(root).keyBindText.visible = false;
         Object(root).keyBindBG.visible = false;
         this.list.dataArray = obj.Animations;
         this.titleBtn.label = this.localization.Title || "None";
         this.list.invalidateData();
      }
      
      protected function setAnimations(obj:Object) : *
      {
         this.titleBtn.label = this.localization.Title || "None";
         this.current_list.dataArray = obj.Animations;
         Object(root).keyBindText.visible = false;
         Object(root).keyBindBG.visible = false;
         this.current_list.invalidateData();
      }
      
      protected function onOptionClick(e:MouseEvent) : *
      {
         if(!this._options)
         {
            GameCommunication.get_settings();
            this.list.visible = true;
            this.sb.visible = true;
            this.current_list.visible = false;
            this.sb_current.visible = false;
            Object(root).toolTip.visible = true;
            this.updatePositionsText();
         }
         else
         {
            GameCommunication.set_settings();
            this.list.visible = false;
            this.sb.visible = false;
            this.current_list.visible = true;
            this.sb_current.visible = true;
            Object(root).toolTip.visible = false;
         }
         this._options = !this._options;
      }
      
      protected function openSettings(obj:Object) : *
      {
         this.setAllAnimations(obj);
         this.list.visible = true;
         this.sb.visible = true;
         this.current_list.visible = false;
         this.sb_current.visible = false;
      }
      
      internal function onEmotionCheck(e:EmotionEvent) : *
      {
         GameCommunication.check_anim(e.id,e.check);
      }
      
      internal function onEmotionClick(e:EmotionEvent) : *
      {
         GameCommunication.play_anim(e.id);
      }
      
      internal function onEmotionKeyChange(e:EmotionEvent) : *
      {
         this._current_sel_num = e.index;
         GameCommunication.change_anim_key(e.id);
         this.showChangeKeyLabel();
      }
      
      internal function showChangeKeyLabel() : *
      {
         Object(root).keyBindText.visible = true;
         Object(root).keyBindBG.visible = true;
         this.updatePositionsText();
      }
      
      internal function updatePositionsText() : *
      {
         Object(root).keyBindBG.width = Object(root).keyBindText.textWidth + 10;
         Object(root).keyBindText.x = Object(root).mainWindow.x + Object(root).mainWindow.width;
         Object(root).keyBindText.y = Object(root).mainWindow.y + 13 + this._current_sel_num * 22;
         Object(root).keyBindBG.x = Object(root).keyBindText.x;
         Object(root).keyBindBG.y = Object(root).keyBindText.y;
         Object(root).toolTip.x = Object(root).mainWindow.x - 100;
         Object(root).toolTip.y = Object(root).mainWindow.y - 100;
      }
      
      internal function onResizeFrame(e:*) : *
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
         this.height = e.height - 12;
         this.width = e.width - 12;
         this.x += (e as ResizeFrameEvent).x;
         this.y += (e as ResizeFrameEvent).y;
         this.X = this.x;
         this.Y = this.y;
         this.X = x;
         this.Y = y;
         invalidateSize();
         this.updatePositionsText();
      }
      
      protected function onCloseButtonClick(e:MouseEvent) : void
      {
         GameCommunication.hide_interface();
      }
      
      protected function onWindowStartDrag(e:Event) : void
      {
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false,0,true);
         stage.addEventListener(MouseEvent.MIDDLE_MOUSE_UP,this.onWindowStopDrag,false,0,true);
         stage.addEventListener(MouseEvent.MOUSE_MOVE,this.onWindowDrag,false,0,true);
         startDrag();
      }
      
      protected function onWindowStopDrag(e:MouseEvent) : void
      {
         var tmp:Boolean = false;
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onWindowStopDrag,false);
         stage.removeEventListener(MouseEvent.MIDDLE_MOUSE_UP,this.onWindowStopDrag);
         stage.removeEventListener(MouseEvent.MOUSE_MOVE,this.onWindowDrag);
         stopDrag();
         this.validatePosition(null);
         GameCommunication.save_position(this.x,this.y);
      }
      
      protected function load_position(args:*) : *
      {
         this.x = args.x;
         this.y = args.y;
         this.validatePosition(null);
      }
      
      protected function onWindowDrag(e:MouseEvent) : void
      {
         this.updatePositionsText();
      }
      
      public function validatePosition(obj:*) : *
      {
         if(y < (Object(root).height - stage.stageHeight) / 2)
         {
            y = (Object(root).height - stage.stageHeight) / 2;
         }
         if(x < (Object(root).width - stage.stageWidth) / 2 - 100)
         {
            x = (Object(root).width - stage.stageWidth) / 2 - 100;
         }
         if(y > (Object(root).height + stage.stageHeight) / 2 - 280)
         {
            y = (Object(root).height + stage.stageHeight) / 2 - 320;
         }
         if(x > (Object(root).width + stage.stageWidth) / 2 - 301)
         {
            x = (Object(root).width + stage.stageWidth) / 2 - 301;
         }
         this.X = this.x;
         this.Y = this.y;
      }
      
      override protected function draw() : void
      {
         if(isInvalid("source"))
         {
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
   }
}


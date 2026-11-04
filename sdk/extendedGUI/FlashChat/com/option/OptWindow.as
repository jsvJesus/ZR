package com.option
{
   import com.ChannelColors;
   import com.colorPicker.ColorEvent;
   import flash.display.MovieClip;
   import flash.events.Event;
   import flash.events.FocusEvent;
   import flash.events.KeyboardEvent;
   import flash.events.MouseEvent;
   import flash.events.TextEvent;
   import flash.text.TextField;
   import flash.text.TextFieldType;
   import flash.ui.Keyboard;
   import flash.utils.setTimeout;
   import scaleform.clik.controls.Button;
   import scaleform.clik.core.UIComponent;
   
   public class OptWindow extends UIComponent
   {
      public var FontLabel:TextField;
      
      public var FontSlider:DefaultSlider;
      
      public var OnlyEng:DefCheckBox;
      
      public var Visual4:DefCheckBox;
      
      public var Visual5:DefCheckBox;
      
      public var Visual6:DefCheckBox;
      
      public var closeBtn:Button;
      
      public var LoadBtn:Button;
      
      public var TabName:TextField;
      
      public var Title:TextField;
      
      public var TitleChan:TextField;
      
      public var TitleWhisp:TextField;
      
      public var Whisp1:DefCheckBox;
      
      public var Whisp2:DefCheckBox;
      
      public var Whisp3:DefCheckBox;
      
      public var TitleBack:TextField;
      
      public var TitleVisual:TextField;
      
      public var Visual1:DefCheckBox;
      
      public var Visual2:DefCheckBox;
      
      public var Visual3:DefCheckBox;
      
      public var BackGame:TextField;
      
      public var BackGameSlider:DefaultSlider;
      
      public var BackChat:TextField;
      
      public var BackChatSlider:DefaultSlider;
      
      public var OptBtn:Button;
      
      public var editBtn:Button;
      
      public var background:MovieClip;
      
      public var hit:MovieClip;
      
      public var newForm:newTabForm;
      
      public var Chan:DefChanals;
      
      public var Colors:ColorsBar;
      
      public var Func:Function;
      
      protected var _type:String;
      
      private var _currendIDColorChange:int = -1;
      
      private var _currendNumColorChange:int = -1;
      
      public function OptWindow()
      {
         super();
      }
      
      public function set type(value:String) : void
      {
         this._type = value;
      }
      
      public function get type() : String
      {
         return this._type;
      }
      
      protected function onFocusInput(e:KeyboardEvent) : *
      {
         if(e.keyCode == Keyboard.ENTER || e.keyCode == Keyboard.NUMPAD_ENTER)
         {
            if(this.Func != null)
            {
               this.removeEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
               this.Func();
            }
         }
      }
      
      public function validateScrollOnChannels() : *
      {
         if(this.Chan.Channels.rowCount < this.Chan.Channels.dataProvider.length)
         {
            this.Chan.sb.visible = true;
         }
         else
         {
            this.Chan.sb.visible = false;
         }
      }
      
      override protected function configUI() : void
      {
         this.Colors.visible = false;
         this.Colors.x = this.width - this.Colors.width;
         this.Colors.CloseBtn.label = Object(root).MainChat.locale.CLOSE;
         this.addEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         this.editBtn.addEventListener(MouseEvent.CLICK,this.changeTitle);
         this.TabName.addEventListener(FocusEvent.FOCUS_OUT,this.lostFocus);
         this.newForm.NickField.visible = false;
         this.newForm.NickField.border = true;
         this.newForm.whispTabChk.addEventListener(MouseEvent.CLICK,this.changeNick);
         this.newForm.NickField.addEventListener(FocusEvent.FOCUS_IN,this.getNickFocus);
         this.newForm.NickField.addEventListener(FocusEvent.FOCUS_OUT,this.lostNickFocus);
         this.newForm.NickField.addEventListener(TextEvent.TEXT_INPUT,this.changeNickTabName);
         this.Chan.Channels.addEventListener(ColorEvent.SHOW,this.showChangerColors);
         this.Colors.addEventListener(ColorEvent.SELECT,this.changeColor);
         this.LoadBtn.addEventListener(MouseEvent.CLICK,this.LoadClick);
         this.Whisp1.addEventListener(Event.SELECT,this.changeWhispEnabled);
         this.TabName.backgroundColor = 2631720;
         this.TabName.background = false;
         this.newForm.NickField.background = true;
         this.newForm.NickField.backgroundColor = 2631720;
         if(!this.Whisp1.selected)
         {
            this.Whisp2.enabled = false;
            this.Whisp3.enabled = false;
         }
         else
         {
            this.Whisp2.enabled = true;
            this.Whisp3.enabled = true;
         }
         setTimeout(this.validateScrollOnChannels,0);
      }
      
      private function showChangerColors(e:ColorEvent) : *
      {
         if(this.Chan.Channels.getRendererAt(this._currendNumColorChange) != null)
         {
            (this.Chan.Channels.getRendererAt(this._currendNumColorChange) as ColorChannelCheckBox).frame = false;
         }
         this._currendIDColorChange = e.id;
         this._currendNumColorChange = e.index;
         this.Colors.Show(e.color,e.index);
      }
      
      private function changeColor(e:ColorEvent) : *
      {
         ChannelColors.setColor(this._currendIDColorChange,e.color);
         (this.Chan.Channels.getRendererAt(this._currendNumColorChange) as ColorChannelCheckBox).color = e.color;
      }
      
      internal function changeWhispEnabled(e:Event) : *
      {
         if(!this.Whisp1.selected)
         {
            this.Whisp2.enabled = false;
            this.Whisp3.enabled = false;
         }
         else
         {
            this.Whisp2.enabled = true;
            this.Whisp3.enabled = true;
         }
      }
      
      internal function LoadClick(e:MouseEvent) : *
      {
         var Obj:Object = null;
         stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         switch(this._type)
         {
            case "new":
               Obj = new Object();
               Obj = this.getNewTabSettings();
               Object(root).MainChat.TabBar.addTab(Obj.setting,Obj.label,"",Obj.user,Obj.whisp,"",this.OnlyEng.selected);
               Object(root).MainChat.closeOptions();
               break;
            case "setting":
               Object(root).MainChat.TabBar.deleteTab(Object(root).MainChat.TabBar.selectedIndex);
               Object(root).MainChat.closeOptions();
               break;
            case "window_setting":
               Object(root).removeChild(parent);
         }
      }
      
      public function getNewTabSettings() : Object
      {
         var zz:Object = null;
         var tmp:Object = null;
         var i:* = undefined;
         var Obj:Object = new Object();
         Obj.label = this.TabName.text;
         Obj.setting = new Array();
         var Tarr:Array = new Array();
         Tarr = this.Chan.Channels.getChannels();
         for(var z:* = 0; z < Tarr.length; z++)
         {
            zz = new Object();
            zz = Tarr[z];
            Obj.setting.push(zz);
         }
         if(this.newForm.whispTabChk.selected)
         {
            Obj.user = this.newForm.NickField.text;
            tmp = new Object();
            Obj.whisp = true;
            tmp.label = this.TabName.text;
            tmp.selected = true;
            tmp.id = -99;
            for(i in Object(root).MainChat.defaultChannals)
            {
               if(Object(root).MainChat.defaultChannals[i].label == Object(root).MainChat.locale.WHISPER)
               {
                  tmp.R = Object(root).MainChat.defaultChannals[i].R;
                  tmp.G = Object(root).MainChat.defaultChannals[i].G;
                  tmp.B = Object(root).MainChat.defaultChannals[i].B;
               }
            }
            tmp.com = this.TabName.text;
            Obj.setting.unshift(tmp);
         }
         else
         {
            Obj.user = "";
            Obj.whisp = false;
         }
         Obj.TEXT = "";
         return Obj;
      }
      
      protected function changeNickTabName(e:TextEvent) : *
      {
         this.TabName.text = "@" + e.target.text + e.text;
      }
      
      protected function getNickFocus(e:FocusEvent) : *
      {
         if(this.newForm.NickField.text == Object(root).MainChat.locale.NICK_FIELD)
         {
            this.newForm.NickField.text = "";
         }
      }
      
      protected function lostNickFocus(e:FocusEvent) : *
      {
         if(this.newForm.NickField.text == "")
         {
            this.newForm.NickField.text = Object(root).MainChat.locale.NICK_FIELD;
         }
      }
      
      protected function changeNick(e:MouseEvent) : *
      {
         this.newForm.NickField.visible = !this.newForm.whispTabChk.selected;
      }
      
      protected function lostFocus(e:FocusEvent) : *
      {
         this.TabName.type = TextFieldType.DYNAMIC;
         this.TabName.selectable = false;
         this.TabName.background = false;
      }
      
      protected function changeTitle(e:MouseEvent) : *
      {
         this.TabName.type = TextFieldType.INPUT;
         this.TabName.background = true;
         this.TabName.selectable = true;
         stage.focus = this.TabName;
      }
      
      protected function closeWind(e:Event) : *
      {
         stage.removeEventListener(KeyboardEvent.KEY_DOWN,this.onFocusInput);
         parent.removeChild(this);
      }
   }
}


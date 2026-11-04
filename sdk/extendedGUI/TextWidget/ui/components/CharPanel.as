package ui.components
{
   import com.dvalimona.components.*;
   import com.dvalimona.utils.*;
   import communication.*;
   import events.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.utils.*;
   import logging.*;
   import ui.*;
   
   public class CharPanel extends Component
   {
      protected var panelWidth:uint = 400;
      
      protected var contentShift:uint = 15;
      
      protected var lineHeight:uint = 25;
      
      protected var lineColor:uint = 11776934;
      
      protected var charColor:uint = 16777215;
      
      protected var inited:Boolean = false;
      
      protected var extended:Boolean = false;
      
      protected var _back:ui.components.BlackPanel;
      
      protected var vBox:VBox;
      
      protected var headerPanel:HeaderPanel;
      
      protected var headerBox:HBox;
      
      protected var headerBoxLeft:HBox;
      
      protected var headerBoxRight:HBox;
      
      protected var _label:LabelShadowed;
      
      protected var serviceButton:PushButton;
      
      protected var _control:ClearButton;
      
      protected var _addCharHint:Text;
      
      protected var linesBox:VBox;
      
      protected var charLine:HBoxLine;
      
      protected var charCaption:Label;
      
      protected var healthLine:HBoxLine;
      
      protected var healthCaption:Label;
      
      protected var healthValue:Label;
      
      protected var staminaLine:HBoxLine;
      
      protected var staminaCaption:Label;
      
      protected var staminaValue:Label;
      
      protected var speedLine:HBoxLine;
      
      protected var speedCaption:Label;
      
      protected var speedValue:Label;
      
      protected var carryingLine:HBoxLine;
      
      protected var carryingCaption:Label;
      
      protected var carryingValue:Label;
      
      public function CharPanel(param1:DisplayObjectContainer = null, param2:Number = 0, param3:Number = 0)
      {
         Character.core.addEventListener(Character.UPDATED,this.onCharactersUpdated);
         super(param1,param2,param3);
      }
      
      override protected function unfreeze() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.unfreeze");
         Character.Update();
         this.panelWidth = Base.stage.stageWidth > 1280 ? 600 : 500;
      }
      
      protected function onCharactersUpdated(param1:Event) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"Character.onCharactersUpdated");
         setTimeout(this.updateContent,50);
      }
      
      override protected function addChildren() : void
      {
         this._back = new BlackPanel();
         this._back.mouseEnabled = false;
         addChild(this._back);
         this.vBox = new VBox();
         this.vBox.padding = 10;
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 1;
         this.vBox.debug = false;
         this.vBox.width = this.panelWidth;
         super.addChild(this.vBox);
         this.headerPanel = new HeaderPanel(this.vBox);
         this.headerPanel.useBackground = false;
         this.headerPanel.label.$ = "extendedGUI.RootWindow.charButton";
         this.headerPanel.label.size = 22;
         this.headerPanel.label.color = 16777215;
         this.headerPanel.height = 46;
         this.headerPanel.width = this.panelWidth - 0;
         this.serviceButton = new ClearButton(this.headerPanel.slot);
         this.serviceButton.$ = "extendedGUI.RootWindow.charControl";
         this.serviceButton.underline = false;
         this.serviceButton.size = 18;
         this.serviceButton.align = Label.RIGHT;
         this.serviceButton.debug = false;
         this.serviceButton.autoWidth = true;
         this.serviceButton.addEventListener(MouseEvent.CLICK,this.serviceClickHandler);
         this.serviceButton.paddingRight = 10;
         this.serviceButton.tabEnabled = true;
         this.serviceButton.tabIndex = 200;
         this._addCharHint = new Text();
         this._addCharHint.editable = false;
         this._addCharHint.selectable = false;
         this._addCharHint.autoHeight = true;
         this._addCharHint.font = Base.FONT_LIGHT;
         this._addCharHint.size = 18;
         this._addCharHint.color = this.lineColor;
         this._addCharHint.paddingLeft = 20;
         this._addCharHint.paddingRight = 10;
         this._addCharHint.paddingBottom = 10;
         this._addCharHint.$ = "extendedGUI.RootWindow.charAddNew";
         this._addCharHint.width = this.panelWidth;
         this._addCharHint.height = 130;
         this._addCharHint.debug = false;
         this._addCharHint.draw();
         this.linesBox = new VBox();
         this.linesBox.paddingTop = 0;
         this.linesBox.paddingBottom = 10;
         this.linesBox.paddingLeft = 10;
         this.linesBox.alignment = VBox.LEFT;
         this.linesBox.spacing = 1;
         this.charLine = new HBoxLine(this.linesBox);
         this.charLine.height = this.lineHeight;
         this.charLine.paddingLeft = this.contentShift;
         this.charLine.width = this.panelWidth - this.contentShift - 10;
         this.charCaption = new Label();
         this.charCaption.size = 20;
         this.charCaption.color = this.charColor;
         this.charCaption.text = "";
         this.charLine.left.addChild(this.charCaption);
         this.healthLine = new HBoxLine(this.linesBox);
         this.healthLine.height = this.lineHeight;
         this.healthLine.paddingLeft = this.contentShift;
         this.healthLine.width = this.panelWidth - this.contentShift - 10;
         this.healthCaption = new Label();
         this.healthCaption.size = 18;
         this.healthCaption.color = this.lineColor;
         this.healthCaption.$ = "extendedGUI.RootWindow.health";
         this.healthValue = new Label();
         this.healthValue.debug = false;
         this.healthValue.size = 18;
         this.healthValue.color = this.lineColor;
         this.healthValue.text = "--";
         this.healthValue.paddingRight = 10;
         this.healthLine.left.addChild(this.healthCaption);
         this.healthLine.right.addChild(this.healthValue);
         this.staminaLine = new HBoxLine(this.linesBox);
         this.staminaLine.height = this.lineHeight;
         this.staminaLine.paddingLeft = this.contentShift;
         this.staminaLine.width = this.panelWidth - this.contentShift - 10;
         this.staminaCaption = new Label();
         this.staminaCaption.size = 18;
         this.staminaCaption.color = this.lineColor;
         this.staminaCaption.$ = "extendedGUI.RootWindow.stamina";
         this.staminaValue = new Label();
         this.staminaValue.size = 18;
         this.staminaValue.color = this.lineColor;
         this.staminaValue.text = "--";
         this.staminaValue.paddingRight = 10;
         this.staminaLine.left.addChild(this.staminaCaption);
         this.staminaLine.right.addChild(this.staminaValue);
         this.speedLine = new HBoxLine(this.linesBox);
         this.speedLine.height = this.lineHeight;
         this.speedLine.paddingLeft = this.contentShift;
         this.speedLine.width = this.panelWidth - this.contentShift - 10;
         this.speedCaption = new Label();
         this.speedCaption.size = 18;
         this.speedCaption.color = this.lineColor;
         this.speedCaption.$ = "extendedGUI.RootWindow.speed";
         this.speedValue = new Label();
         this.speedValue.size = 18;
         this.speedValue.color = this.lineColor;
         this.speedValue.text = "--";
         this.speedValue.paddingRight = 10;
         this.speedLine.left.addChild(this.speedCaption);
         this.speedLine.right.addChild(this.speedValue);
         this.carryingLine = new HBoxLine(this.linesBox);
         this.carryingLine.height = this.lineHeight;
         this.carryingLine.paddingLeft = this.contentShift;
         this.carryingLine.width = this.panelWidth - this.contentShift - 10;
         this.carryingCaption = new Label();
         this.carryingCaption.size = 18;
         this.carryingCaption.color = this.lineColor;
         this.carryingCaption.$ = "extendedGUI.RootWindow.carrying";
         this.carryingValue = new Label();
         this.carryingValue.size = 18;
         this.carryingValue.color = this.lineColor;
         this.carryingValue.text = "--";
         this.carryingValue.paddingRight = 10;
         this.carryingLine.left.addChild(this.carryingCaption);
         this.carryingLine.right.addChild(this.carryingValue);
         this.inited = true;
      }
      
      protected function serviceClickHandler(param1:MouseEvent) : void
      {
         Logger.LogToChannel(Logger.DEBUG,"serviceClickHandler");
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.CHAR_SCREEN));
      }
      
      protected function updateContent() : void
      {
         Logger.LogToChannel(Logger.DEBUG,"CharPanel updateContent",Character.list);
         try
         {
            this.extended = Character.list.length > 0;
            if(!this.inited)
            {
               setTimeout(this.updateContent,100);
               return;
            }
            if(this.extended)
            {
               if(this.vBox.contains(this._addCharHint))
               {
                  this.vBox.removeChild(this._addCharHint);
               }
               this.healthValue.text = !!Character.current ? String(NumberUtils.RoundWithPrecision(Character.current.maxHp,2)) : "n/a";
               this.staminaValue.text = !!Character.current ? String(NumberUtils.RoundWithPrecision(Character.current.maxStamina,2)) : "n/a";
               this.speedValue.text = !!Character.current ? String(NumberUtils.RoundWithPrecision(Character.current.maxSpeed,2)) : "n/a";
               this.carryingValue.text = !!Character.current ? String(NumberUtils.RoundWithPrecision(Character.current.maxWeight,2)) : "n/a";
               this.charCaption.text = !!Character.current ? String(Character.current.name) : "n/a";
               this.serviceButton.enabled = true;
               this.linesBox.draw();
               if(!this.vBox.contains(this.linesBox))
               {
                  this.vBox.addChild(this.linesBox);
               }
               this.vBox.draw();
               this.setSize(Navigator.LeftWidth,this.vBox.height);
            }
            else
            {
               if(this.vBox.contains(this.linesBox))
               {
                  this.vBox.removeChild(this.linesBox);
               }
               if(!this.vBox.contains(this._addCharHint))
               {
                  this.vBox.addChild(this._addCharHint);
               }
               this.serviceButton.enabled = true;
               this.vBox.draw();
               this.setSize(Navigator.LeftWidth,this.vBox.height);
            }
            this.draw();
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharPanel updateContent error",error);
         }
      }
      
      override public function draw() : void
      {
         super.draw();
         this._back.width = _width;
         this._back.height = _height;
         this.headerPanel.width = Navigator.LeftWidth - 0;
         this.healthLine.width = Navigator.LeftWidth - this.contentShift - 10;
         this.charLine.width = Navigator.LeftWidth - this.contentShift - 10;
         this.carryingLine.width = Navigator.LeftWidth - this.contentShift - 10;
         this.speedLine.width = Navigator.LeftWidth - this.contentShift - 10;
         this.staminaLine.width = Navigator.LeftWidth - this.contentShift - 10;
      }
   }
}


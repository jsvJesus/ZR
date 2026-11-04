package ui.components
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.display.DisplayObjectContainer;
   import flash.events.*;
   import flash.ui.*;
   import flash.utils.*;
   import lang.*;
   
   public class CharacterPanel2 extends Component
   {
      public static var EDIT_ENABLED:Boolean = false;
      
      public var WIDTH:Number = 413;
      
      protected var mainBox:VBox;
      
      protected var _selected:Boolean;
      
      protected var _character:Character;
      
      protected var charButton:MenuButton2;
      
      protected var newCharacter:MenuButton2;
      
      private var delete_btn:TrashButton;
      
      private var restore_btn:RestoreButton;
      
      private var restoreLabel:LabelShadowed;
      
      public function CharacterPanel2(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.charButton = new MenuButton2(this);
         this.charButton.autoWidth = false;
         this.charButton.height = 37;
         this.charButton.width = this.WIDTH;
         this.charButton.size = 23;
         this.charButton.paddingLeft = 0;
         this.charButton.upColor = 2565927;
         this.charButton.overColorAlpha = 0.4;
         this.charButton.overColor = 2565927;
         this.charButton.marginLeft = 15;
         this.charButton.draw();
         this.charButton.addEventListener(MouseEvent.CLICK,this.onSelect);
         this.newCharacter = new MenuButton2(this);
         this.newCharacter.$ = "extendedGUI.CharWindow.createChar";
         this.newCharacter.height = 37;
         this.newCharacter.width = this.WIDTH;
         this.newCharacter.paddingLeft = 0;
         this.newCharacter.marginLeft = 45;
         this.newCharacter.size = 23;
         this.newCharacter.labelUpColor = 11907244;
         this.newCharacter.overColorAlpha = 0.4;
         this.newCharacter.overColor = 2565927;
         this.newCharacter.autoWidth = false;
         this.newCharacter.addEventListener(MouseEvent.CLICK,this.onCreate);
         this.delete_btn = new TrashButton();
         this.delete_btn.visible = false;
         this.delete_btn.addEventListener(MouseEvent.CLICK,this.delete_restore_char);
         this.delete_btn.y = 37 / 2;
         this.delete_btn.x = this.width - this.delete_btn.width / 2 - 5;
         this.addChild(this.delete_btn);
         this.restore_btn = new RestoreButton();
         this.restore_btn.visible = false;
         this.restore_btn.addEventListener(MouseEvent.CLICK,this.delete_restore_char);
         this.restore_btn.y = 37 / 2;
         this.restore_btn.x = this.width - this.restore_btn.width / 2 - 5;
         this.addChild(this.restore_btn);
         this.restoreLabel = new LabelShadowed(this);
         this.restoreLabel.shadowColor = 0;
         this.restoreLabel.color = 8947848;
         this.restoreLabel.align = Label.RIGHT;
         this.restoreLabel.shadowAlpha = 0.9;
         this.restoreLabel.shadowSize = 1;
         this.restoreLabel.size = 16;
         this.restoreLabel.autoSize = true;
         this.restoreLabel.visible = false;
         this.restoreLabel.paddingLeft = 10;
         this.restoreLabel.paddingTop = 2;
         this.restoreLabel.x = 270;
         this.restoreLabel.y = 8;
         invalidate();
      }
      
      private function getRestoreLabelText() : String
      {
         var result:String = "";
         return Locale.getById("extendedGUI.CharWindow.deleteAfter") + " " + this.getTimeToDelete();
      }
      
      private function getTimeToDelete() : String
      {
         var days:uint = 0;
         var hours:uint = 0;
         var minutes:uint = 0;
         var result:String = "";
         var timeToRemain:uint = uint(this.character.deletionRemainingTime * 1);
         days = timeToRemain / (60 * 60 * 24);
         hours = timeToRemain / (60 * 60);
         minutes = timeToRemain / 60;
         if(days >= 1)
         {
            result = days + Locale.getById("extendedGUI.CharWindow.days");
         }
         else if(hours >= 1)
         {
            result = hours + Locale.getById("extendedGUI.CharWindow.hours");
         }
         else
         {
            result = minutes + Locale.getById("extendedGUI.CharWindow.minutes");
         }
         return result;
      }
      
      public function get state() : String
      {
         var _state:String = null;
         if(this.character == null)
         {
            _state = CharacterState.EMPTY;
         }
         else if(this.character.isNotDeleted)
         {
            _state = CharacterState.NORMAL;
         }
         else
         {
            _state = CharacterState.DELETED;
         }
         return _state;
      }
      
      protected function call_paint(event:MouseEvent) : *
      {
         Api.call(Api.DONAT_PAINT_CHAR);
      }
      
      protected function call_change_face(event:MouseEvent) : *
      {
         modFaceBtn.enabled = false;
         setTimeout(Base.navigator.showCreateCharScreen,100,Base.navigator.getScreen(MainMenuGUI.ROOT_SCREEN),this.character.name,true,true);
      }
      
      protected function delete_restore_char(event:MouseEvent) : *
      {
         if(this.state == CharacterState.NORMAL)
         {
            Base.navigator.showDialog("extendedGUI.RootWindow.deleteCharTitle","extendedGUI.RootWindow.deleteCharText",true,[new DialogButtonItem("extendedGUI.Dialogs.Delete",Character.DeleteLastSelectedChar,0.5,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.Cancel",null,0.5,[Keyboard.ESCAPE])],500,300,false,true,true);
         }
         else if(this.state == CharacterState.DELETED)
         {
            Character.RestoreLastSelectedChar();
         }
      }
      
      private function onCharacterChange() : void
      {
         var before:int = int(getTimer());
         this.restore_btn.visible = false;
         this.delete_btn.visible = false;
         switch(this.state)
         {
            case CharacterState.EMPTY:
               this.asEmpty();
               break;
            case CharacterState.NORMAL:
               this.asNormal();
               break;
            case CharacterState.DELETED:
               this.asDeleted();
         }
         if(this._selected)
         {
            this.charButton.labelUpColor = 16777215;
            this.charButton.mouseEnabled = false;
            this.charButton.upColorAlpha = 0.63;
         }
         else
         {
            this.charButton.mouseEnabled = true;
            this.charButton.labelUpColor = 11907244;
            this.charButton.upColorAlpha = 0;
         }
         this.invalidate();
      }
      
      public function edidCharBtnEnabled(value:Boolean) : *
      {
      }
      
      protected function get allowDelete() : Boolean
      {
         return Boolean(this._character) && Boolean(this._character.deletionRemainingTime) ? this._character.deletionRemainingTime < 0 : false;
      }
      
      protected function get allowRestore() : Boolean
      {
         return Boolean(this._character) && Boolean(this._character.deletionRemainingTime) ? this._character.deletionRemainingTime >= 0 : false;
      }
      
      protected function onCreate(event:MouseEvent) : *
      {
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.NEW_CHAR_SCREEN));
      }
      
      public function asEmpty() : *
      {
         this.selected = false;
         this.newCharacter.visible = true;
         this.charButton.visible = false;
         this.charButton.mouseEnabled = false;
         this.restoreLabel.visible = false;
         this.restore_btn.visible = false;
         this.delete_btn.visible = false;
      }
      
      public function asNormal() : *
      {
         this.charButton.label = this.character.name;
         this.newCharacter.visible = false;
         this.charButton.visible = true;
         if(this.allowDelete)
         {
            this.restoreLabel.visible = false;
            this.delete_btn.visible = this.selected;
            this.delete_btn.x = this.width - this.delete_btn.width / 2 - 5;
         }
      }
      
      public function asDeleted() : *
      {
         this.charButton.label = this.character.name;
         this.newCharacter.visible = false;
         this.charButton.visible = true;
         if(this.allowRestore)
         {
            this.restoreLabel.visible = true;
            this.restoreLabel.text = this.getRestoreLabelText();
            this.restore_btn.visible = true;
            this.restore_btn.x = this.width - this.restore_btn.width - 5;
            this.restoreLabel.x = this.restore_btn.x - this.restoreLabel.width - 5;
         }
      }
      
      protected function onSelect(event:MouseEvent) : *
      {
         if(this.selected)
         {
            return;
         }
         if(this.character == null)
         {
            return;
         }
         if(this.character.is_old)
         {
            Base.navigator.showDialog("extendedGUI.RootWindow.charOldUpdateTitle","extendedGUI.RootWindow.charOldUpdate",true,[new DialogButtonItem("extendedGUI.Dialogs.Yes",this.startUpdateCharacter,0.4,[Keyboard.ENTER]),new DialogButtonItem("extendedGUI.Dialogs.No",null,0.6,[Keyboard.ESCAPE])],500,300,false,true,true);
            return;
         }
         this.selected = true;
         setTimeout(this.setOwnerCharAsCurrent,30,this.character.id);
         dispatchEvent(new Event(Event.SELECT));
      }
      
      private function startUpdateCharacter() : *
      {
         setTimeout(this.setOwnerCharAsCurrent,30,this.character.id);
         setTimeout(Base.navigator.showCreateCharScreen,100,Base.navigator.getScreen(MainMenuGUI.ROOT_SCREEN),this.character.name,true);
      }
      
      private function setOwnerCharAsCurrent(target_id:Number) : void
      {
         Character.selectById(target_id);
      }
      
      public function set selected(value:Boolean) : *
      {
         if(this.selected == value)
         {
            return;
         }
         this._selected = value;
         this.onCharacterChange();
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      public function set_character(value:Character) : *
      {
         if(this.character == value)
         {
            return;
         }
         this._character = value;
         this.onCharacterChange();
      }
      
      public function get character() : Character
      {
         return this._character;
      }
      
      override public function draw() : void
      {
         super.draw();
         this.delete_btn.x = this.width - this.delete_btn.width / 2 - 5;
         this.restore_btn.x = this.width - this.restore_btn.width / 2 - 5;
         this.restoreLabel.x = this.restore_btn.x - this.restoreLabel.width - this.restore_btn.width / 2 - 5;
      }
   }
}


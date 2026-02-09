#!/bin/bash

DB_CONTAINER_NAME=$(docker-compose ps -q db)
DB_USER="secureauthuser"
DB_NAME="secureauthdb"
BACKUP_DIR="/root/backups"

mkdir -p $BACKUP_DIR

BACKUP_FILE="$BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"

docker exec $DB_CONTAINER_NAME pg_dump -U $DB_USER -d $DB_NAME > $BACKUP_FILE

echo "Backup complete! File saved to: $BACKUP_FILE"
echo "To download, use WinSCP/FileZilla or run: scp root@80.90.187.200:$BACKUP_FILE ."